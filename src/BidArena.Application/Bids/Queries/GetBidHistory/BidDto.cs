namespace BidArena.Application.Bids.Queries.GetBidHistory;

public record BidDto(
    Guid Id,
    Guid BidderId,
    decimal Amount,
    bool IsProxy,
    DateTime CreatedAt
);