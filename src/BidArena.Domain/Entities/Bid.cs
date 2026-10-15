namespace BidArena.Domain.Entities;

public class Bid
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public Guid BidderId { get; set; }

    public decimal Amount { get; set; }

    public bool IsProxy { get; set; }
    public decimal? ProxyMaxAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}