using BidArena.Domain.Entities;

namespace BidArena.Domain;

// Holandska aukcija: cena pada za BidIncrement svakog minuta od StartTime,
// ali nikad ispod donje granice (ReservePrice, a ako ga nema, BidIncrement).
public static class DutchPricing
{
    public const int StepMinutes = 1;

    public static decimal Floor(Auction auction) => auction.ReservePrice ?? auction.BidIncrement;

    public static decimal CurrentPrice(Auction auction, DateTime utcNow)
    {
        var elapsed = utcNow - auction.StartTime;
        var steps = elapsed <= TimeSpan.Zero ? 0 : (long)(elapsed.TotalMinutes / StepMinutes);
        var price = auction.StartingPrice - steps * auction.BidIncrement;
        var floor = Floor(auction);
        return price < floor ? floor : price;
    }
}
