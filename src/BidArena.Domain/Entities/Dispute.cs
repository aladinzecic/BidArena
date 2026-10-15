namespace BidArena.Domain.Entities;

public enum DisputeStatus
{
    Open,
    UnderReview,
    Resolved
}

public class Dispute
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public Guid OpenedByUserId { get; set; }

    public string Reason { get; set; } = string.Empty;
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string? Resolution { get; set; }
    public Guid? ResolvedByAdminId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Slike priložene uz spor (dokaz: oštećenje, neodgovarajući predmet...).
    public List<DisputePhoto> Photos { get; set; } = new();
}

public class DisputePhoto
{
    public Guid Id { get; set; }
    public Guid DisputeId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}