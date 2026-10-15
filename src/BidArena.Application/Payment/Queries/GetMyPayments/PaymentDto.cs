namespace BidArena.Application.Payments.Queries.GetMyPayments;

public record PaymentDto(
    Guid Id,
    Guid AuctionId,
    decimal Amount,
    decimal Commission,
    string Status,
    DateTime DueDate,
    DateTime? PaidAt
);