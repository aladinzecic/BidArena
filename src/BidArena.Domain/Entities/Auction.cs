namespace BidArena.Domain.Entities;

public enum AuctionType
{
    English,
    Dutch
}

public enum AuctionStatus
{
    Draft,
    Scheduled,
    Active,
    Ended,
    Settled,
    Disputed,
    Cancelled
}

public class Auction
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }

    public AuctionType Type { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Draft;

    public decimal StartingPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal BidIncrement { get; set; }

    public decimal CurrentPrice { get; set; }
    public Guid? CurrentWinnerId { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public int AntiSnipeWindowMinutes { get; set; } = 5;
    public int AntiSnipeExtensionMinutes { get; set; } = 10;

    public uint Version { get; set; }

    public List<Bid> Bids { get; set; } = new();
}