namespace BidArena.Domain.Entities;

public enum PaymentStatus
{
    Pending,
    Paid,
    Overdue,
    Refunded
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public Guid WinnerId { get; set; }

    public decimal Amount { get; set; }
    public decimal Commission { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
}