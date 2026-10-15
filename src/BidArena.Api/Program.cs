using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;
using BidArena.Application.Items.Commands.CreateItem;
using BidArena.Application.Common.Interfaces;
using BidArena.Infrastructure.Repositories;
using Scalar.AspNetCore;
using BidArena.Api.Middleware;
using BidArena.Application.Common.Behaviors;
using FluentValidation;
using BidArena.Infrastructure.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using BidArena.Api.BackgroundServices;
using BidArena.Application.Auctions.Mappings;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

// Bootstrap logger: hvata greške koje nastanu pre nego što se učita konfiguracija (npr. nedostaje Jwt:Key).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();
    builder.Services.AddMemoryCache();
    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();   
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(CreateItemCommand).Assembly);
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });

    builder.Services.AddValidatorsFromAssembly(typeof(CreateItemCommand).Assembly);
    builder.Services.AddAutoMapper(cfg => { }, typeof(AuctionMappingProfile));
    builder.Services.AddScoped<IItemRepository, ItemRepository>();
    builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
    builder.Services.AddScoped<IBidRepository, BidRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    builder.Services.AddScoped<IRatingRepository, RatingRepository>();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Nedostaje ConnectionStrings:DefaultConnection u konfiguraciji");

    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "postgresql", tags: new[] { "db", "ready" });

    var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
        ?? throw new InvalidOperationException("Nedostaje Jwt sekcija u konfiguraciji");

    if (string.IsNullOrWhiteSpace(jwtSettings.Key))
        throw new InvalidOperationException("Nedostaje Jwt:Key (postavi ga preko dotnet user-secrets)");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false; // claim-ovi ostaju "sub", "role", "email"
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = "name",
                RoleClaimType = "role"
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
    builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
    builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
    builder.Services.AddScoped<IDisputeRepository, DisputeRepository>();
    builder.Services.AddScoped<IAdminStatsRepository, AdminStatsRepository>();
    builder.Services.AddHostedService<AuctionClosingWorker>();
    builder.Services.AddHostedService<PaymentOverdueWorker>();
    builder.Services.AddHostedService<AuctionStartingWorker>();
    builder.Services.AddHostedService<ShipmentOverdueWorker>();
    builder.Services.AddHostedService<AuctionEndingSoonWorker>();

    // Otpremanje slika predmeta (lokalni disk; folder se servira kao statički sadržaj na /uploads).
    builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorage"));
    // FileStorage:Provider = "R2" prebacuje čuvanje slika na Cloudflare R2 (za hosting bez trajnog diska).
    if (string.Equals(builder.Configuration["FileStorage:Provider"], "R2", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.Configure<R2StorageSettings>(builder.Configuration.GetSection("R2"));
        builder.Services.AddSingleton<IFileStorage, R2FileStorage>();
    }
    else
    {
        builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
    }

    // CORS za frontend (SPA). Origin-i se podešavaju u Cors:AllowedOrigins (appsettings ili Cors__AllowedOrigins__0 env).
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:3000", "http://localhost:4200", "http://localhost:5173" };
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

    // Rate limiting za prijavu/registraciju (zaštita od brute-force napada): 10 zahteva u minuti po IP adresi.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    });

    // Iza reverse proxy-ja (Render, Caddy...) stvarna IP adresa i šema dolaze iz X-Forwarded-* zaglavlja;
    // bez toga bi rate limiter svim klijentima video IP adresu proxy-ja. Uključuje se sa ForwardedHeaders__Enabled=true.
    var forwardedHeadersEnabled = builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");
    if (forwardedHeadersEnabled)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    var app = builder.Build();

    if (forwardedHeadersEnabled)
        app.UseForwardedHeaders();

    // Migracije se primenjuju samo ako je izričito uključeno (Database:MigrateOnStartup=true).
    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Log.Information("Primena EF migracija pri startu (Database:MigrateOnStartup=true)");
        dbContext.Database.Migrate();
    }

    // Request logging ide ispred exception middleware-a: loguje se konačan status odgovora,
    // a izuzetke već loguje ExceptionHandlingMiddleware (bez dupliranja stack trace-ova).
    app.UseSerilogRequestLogging(options =>
    {
        // Health check poziva (npr. iz Docker-a) ne zatrpavaju log na Information nivou.
        options.GetLevel = (httpContext, _, ex) =>
            ex is not null || httpContext.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : httpContext.Request.Path.StartsWithSegments("/health")
                    ? LogEventLevel.Debug
                    : LogEventLevel.Information;
    });
    app.UseExceptionHandling();
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();

    var storageSettings = app.Configuration.GetSection("FileStorage").Get<FileStorageSettings>()
        ?? new FileStorageSettings();
    var uploadsRoot = Path.GetFullPath(storageSettings.RootPath);
    Directory.CreateDirectory(uploadsRoot);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsRoot),
        RequestPath = "/" + storageSettings.PublicPath.Trim('/')
    });
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    // Posle autentikacije/autorizacije (neprijavljen korisnik ne saznaje fazu aukcije), a pre izvršavanja endpoint-a.
    app.UseAuctionPhaseEnforcement();
    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous();

    app.Run();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Aplikacija se nije mogla pokrenuti");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
