using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BidArena.Api.Middleware;

/// <summary>
/// Auction phase enforcement middleware. Za endpoint-e označene sa <see cref="AllowedPhasesAttribute"/>
/// izvlači auctionId iz rute ({id}), učitava trenutnu fazu aukcije (kratkotrajni keš, pa baza) i poredi je
/// sa dozvoljenim fazama. Pogrešna faza vraća 409 Conflict (Problem Details) bez ulaska u kontroler/handler,
/// a nepostojeća aukcija 404. Mora biti registrovan posle UseRouting (implicitno) i UseAuthorization,
/// a pre MapControllers, da bi endpoint i njegovi metapodaci već bili poznati.
/// </summary>
public sealed class AuctionPhaseMiddleware
{
    private const string ProblemJson = "application/problem+json";
    private const string RouteKey = "id";

    // Kratak TTL: faza se menja (workeri, Activate/Cancel), a handler svakako ponovo proverava stanje u transakciji.
    internal static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

    private readonly RequestDelegate _next;
    private readonly ILogger<AuctionPhaseMiddleware> _logger;

    public AuctionPhaseMiddleware(RequestDelegate next, ILogger<AuctionPhaseMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Scoped zavisnosti (repozitorijum) dolaze kao parametri InvokeAsync, jer je middleware singleton.
    public async Task InvokeAsync(HttpContext context, IAuctionRepository auctions, IMemoryCache cache)
    {
        var attribute = context.GetEndpoint()?.Metadata.GetMetadata<AllowedPhasesAttribute>();

        if (attribute is null
            || !context.Request.RouteValues.TryGetValue(RouteKey, out var rawId)
            || !Guid.TryParse(rawId?.ToString(), out var auctionId))
        {
            await _next(context);
            return;
        }

        var status = await GetPhaseAsync(auctionId, auctions, cache, context.RequestAborted);

        if (status is null)
        {
            _logger.LogWarning("Aukcija {AuctionId} ne postoji ({Method} {Path})",
                auctionId, context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, StatusCodes.Status404NotFound,
                "Aukcija nije pronađena", $"Aukcija {auctionId} ne postoji");
            return;
        }

        if (!attribute.Phases.Contains(status.Value))
        {
            var allowed = string.Join(", ", attribute.Phases);

            _logger.LogWarning(
                "Akcija odbijena: aukcija {AuctionId} je u fazi {Phase}, dozvoljeno: {Allowed} ({Method} {Path})",
                auctionId, status.Value, allowed, context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                "Akcija nije dozvoljena u trenutnoj fazi aukcije",
                $"Aukcija je u statusu {status.Value}, a akcija je dozvoljena samo u: {allowed}");
            return;
        }

        await _next(context);
    }

    private static async Task<AuctionStatus?> GetPhaseAsync(
        Guid auctionId, IAuctionRepository auctions, IMemoryCache cache, CancellationToken cancellationToken)
    {
        var key = CacheKey(auctionId);

        if (cache.TryGetValue(key, out AuctionStatus cached))
            return cached;

        var status = await auctions.GetStatusAsync(auctionId, cancellationToken);

        // Nepostojeća aukcija se ne kešira.
        if (status is not null)
            cache.Set(key, status.Value, CacheDuration);

        return status;
    }

    /// <summary>Ključ keša faze aukcije; može se koristiti za eksplicitno brisanje (cache.Remove) pri promeni statusa.</summary>
    public static string CacheKey(Guid auctionId) => $"auction-phase:{auctionId}";

    private static Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = status;

        return context.Response.WriteAsJsonAsync(
            problem, options: null, contentType: ProblemJson, cancellationToken: context.RequestAborted);
    }
}
