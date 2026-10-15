namespace BidArena.Domain.Entities;

// Ocena druge strane nakon uspešno završene aukcije (Settled). Jedna ocena po korisniku i aukciji.
public class Rating
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public Guid RaterId { get; set; }
    public Guid RatedUserId { get; set; }

    public int Score { get; set; } // 1-5
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
