namespace BidArena.Api.Middleware;

public static class AuctionPhaseMiddlewareExtensions
{
    public static IApplicationBuilder UseAuctionPhaseEnforcement(this IApplicationBuilder app)
        => app.UseMiddleware<AuctionPhaseMiddleware>();
}
