using System.Data.Common;
using System.Text.Json;
using BidArena.Api.Middleware;
using BidArena.Application.Common.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BidArena.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private const string ProblemJson = "application/problem+json";

    public static TheoryData<Exception, int> ExceptionsAndStatuses => new()
    {
        { new ArgumentException("Neispravan argument"), 400 },
        { new UnauthorizedAccessException("Pogrešni kredencijali"), 401 },
        { new ForbiddenAccessException("Nisi vlasnik aukcije"), 403 },
        { new KeyNotFoundException("Aukcija ne postoji"), 404 },
        { new InvalidOperationException("Aukcija nije aktivna"), 409 },
        { new DbUpdateConcurrencyException("Konflikt verzija"), 409 },
        { new BadHttpRequestException("Request body too large.", 413), 413 },
    };

    private static ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/Auctions/1/bids";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<DefaultHttpContext> RunAsync(Exception exception, DefaultHttpContext? context = null)
    {
        context ??= CreateContext();
        await CreateMiddleware(_ => throw exception).InvokeAsync(context);
        return context;
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }

    [Theory]
    [MemberData(nameof(ExceptionsAndStatuses))]
    public async Task Invoke_MapsExceptionToProblemDetails(Exception exception, int expectedStatus)
    {
        var context = await RunAsync(exception);

        context.Response.StatusCode.Should().Be(expectedStatus);
        context.Response.ContentType.Should().Be(ProblemJson);

        var body = await ReadBodyAsync(context);
        body.GetProperty("status").GetInt32().Should().Be(expectedStatus);
        body.GetProperty("detail").GetString().Should().Be(exception.Message);
        body.GetProperty("instance").GetString().Should().Be("/api/v1/Auctions/1/bids");
        body.GetProperty("traceId").GetString().Should().Be("trace-1");
    }

    [Fact]
    public async Task Invoke_UnexpectedException_Returns500WithoutLeakingDetails()
    {
        var context = await RunAsync(new Exception("lozinka baze je 1234"));

        context.Response.StatusCode.Should().Be(500);

        var body = await ReadBodyAsync(context);
        body.TryGetProperty("detail", out _).Should().BeFalse();
        body.ToString().Should().NotContain("1234");
    }

    [Fact]
    public async Task Invoke_ValidationException_Returns400WithErrorsGroupedByProperty()
    {
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Email", "Email je obavezan"),
            new ValidationFailure("Email", "Email nije ispravan"),
            new ValidationFailure("Password", "Lozinka je prekratka"),
        });

        var context = await RunAsync(exception);

        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Be(ProblemJson);

        var errors = (await ReadBodyAsync(context)).GetProperty("errors");
        errors.GetProperty("Email").GetArrayLength().Should().Be(2);
        errors.GetProperty("Password").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Invoke_TransientDatabaseFailureWrappedByEf_Returns503NotConflict()
    {
        // EF Core pad baze umotava u InvalidOperationException; to ne sme da završi kao 409.
        var exception = new InvalidOperationException(
            "An exception has been raised that is likely due to a transient failure.",
            new FakeDbException("connection refused", isTransient: true));

        var context = await RunAsync(exception);

        context.Response.StatusCode.Should().Be(503);
        (await ReadBodyAsync(context)).TryGetProperty("detail", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Invoke_Timeout_Returns503()
    {
        var context = await RunAsync(new TimeoutException("istekao timeout"));

        context.Response.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task Invoke_PermanentDatabaseError_Returns500()
    {
        var exception = new DbUpdateException("duplicate key", new FakeDbException("23505", isTransient: false));

        var context = await RunAsync(exception);

        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Invoke_NoException_PassesThroughUntouched()
    {
        var context = CreateContext();
        var called = false;

        await CreateMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        called.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task Invoke_ClientAbortedRequest_Returns499WithoutBody()
    {
        var context = CreateContext();
        context.RequestAborted = new CancellationToken(canceled: true);

        await CreateMiddleware(_ => throw new OperationCanceledException()).InvokeAsync(context);

        context.Response.StatusCode.Should().Be(499);
        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task Invoke_CancellationNotCausedByClient_IsTreatedAsServerError()
    {
        var context = await RunAsync(new OperationCanceledException("timeout drugog servisa"));

        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Invoke_ResponseAlreadyStarted_RethrowsInsteadOfWriting()
    {
        var context = CreateContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var act = () => CreateMiddleware(_ => throw new InvalidOperationException("prekasno")).InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("prekasno");
        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task UseExceptionHandling_RegistersMiddlewareInPipeline()
    {
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var app = new ApplicationBuilder(services);
        app.UseExceptionHandling();
        app.Run(_ => throw new KeyNotFoundException("Aukcija ne postoji"));
        var pipeline = app.Build();

        var context = CreateContext();
        context.RequestServices = services;

        await pipeline(context);

        context.Response.StatusCode.Should().Be(404);
        context.Response.ContentType.Should().Be(ProblemJson);
    }

    private sealed class FakeDbException : DbException
    {
        private readonly bool _isTransient;

        public FakeDbException(string message, bool isTransient) : base(message)
        {
            _isTransient = isTransient;
        }

        public override bool IsTransient => _isTransient;
    }

    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }
}
