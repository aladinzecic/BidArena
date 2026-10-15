namespace BidArena.Domain.Entities;

public enum NotificationType
{
    Outbid,
    AuctionEndingSoon,
    AuctionWon,
    PaymentDue,
    ItemShipped,
    PaymentReceived,
    DisputeOpened,
    DisputeResolved,
    ItemDelivered,
    AuctionCancelled,
    QuestionAsked,
    QuestionAnswered
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    // Aukcija na koju se obaveštenje odnosi (koristi se i za sprečavanje duplih obaveštenja).
    public Guid? AuctionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}