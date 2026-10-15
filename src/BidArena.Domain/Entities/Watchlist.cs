namespace BidArena.Domain.Entities;

public class WatchlistItem
{
    public Guid UserId { get; set; }
    public Guid AuctionId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}