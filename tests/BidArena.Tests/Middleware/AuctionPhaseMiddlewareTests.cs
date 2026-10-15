using System.Text.Json;
using BidArena.Api.Middleware;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BidArena.Tests.Middleware;

public class AuctionPhaseMiddlewareTests
{
    private readonly Mock<IAuctionRepository> _auctions = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Guid _auctionId = Guid.NewGuid();
    private bool _nextCalled;

    private async Task<DefaultHttpContext> RunAsync(
        AllowedPhasesAttribute? attribute, object? routeId = null, IMemoryCache? cache = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = $"/api/v1/Auctions/{_auctionId}/bids";
        context.Response.Body = new MemoryStream();
        context.Request.RouteValues = new RouteValueDictionary { ["id"] = routeId ?? _auctionId.ToString() };

        var metadata = attribute is null
            ? new EndpointMetadataCollection()
            : new EndpointMetadataCollection(attribute);
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "test"));

        var middleware = new AuctionPhaseMiddleware(
            _ => { _nextCalled = true; return Task.CompletedTask; },
            NullLogger<AuctionPhaseMiddleware>.Instance);

        await middleware.InvokeAsync(context, _auctions.Object, cache ?? _cache);
        return context;
    }

    private void SetupStatus(AuctionStatus? status)
        => _auctions.Setup(a => a.GetStatusAsync(_auctionId, It.IsAny<CancellationToken>())).ReturnsAsync(status);

    private static async Task<JsonElement> BodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task WrongPhase_Returns409_WithoutCallingNext()
    {
        SetupStatus(AuctionStatus.Ended);

        var context = await RunAsync(new AllowedPhasesAttribute(AuctionStatus.Active));

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(409);
        context.Response.ContentType.Should().StartWith("application/problem+json");
        var body = await BodyAsync(context);
        body.GetProperty("status").GetInt32().Should().Be(409);
        body.GetProperty("detail").GetString().Should().Contain("Ended").And.Contain("Active");
    }

    [Fact]
    public async Task AllowedPhase_CallsNext()
    {
        SetupStatus(AuctionStatus.Active);

        var context = await RunAsync(new AllowedPhasesAttribute(AuctionStatus.Draft, AuctionStatus.Active));

        _nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UnknownAuction_Returns404_WithoutCallingNext()
    {
        SetupStatus(null);

        var context = await RunAsync(new AllowedPhasesAttribute(AuctionStatus.Active));

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task EndpointWithoutAttribute_PassesThrough_WithoutQueryingDatabase()
    {
        await RunAsync(attribute: null);

        _nextCalled.Should().BeTrue();
        _auctions.Verify(a => a.GetStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NonGuidRouteId_PassesThrough()
    {
        await RunAsync(new AllowedPhasesAttribute(AuctionStatus.Active), routeId: "abc");

        _nextCalled.Should().BeTrue();
        _auctions.Verify(a => a.GetStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Phase_IsCached_BetweenRequests()
    {
        SetupStatus(AuctionStatus.Active);

        await RunAsync(new AllowedPhasesAttribute(AuctionStatus.Active));
        await RunAsync(new AllowedPhasesAttribute(AuctionStatus.Active));

        _auctions.Verify(a => a.GetStatusAsync(_auctionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvictedCacheEntry_IsReloadedFromRepository()
    {
        SetupStatus(AuctionStatus.Active);
        await RunAsync(new AllowedPhasesAttribute(AuctionStatus.Active));

        _cache.Remove(AuctionPhaseMiddleware.CacheKey(_auctionId));
        SetupStatus(AuctionStatus.Ended);

        var context = await RunAsync(new AllowedPhasesAttribute(AuctionStatus.Active));

        context.Response.StatusCode.Should().Be(409);
    }
}
