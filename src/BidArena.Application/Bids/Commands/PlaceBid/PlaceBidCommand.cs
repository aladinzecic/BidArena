using MediatR;

namespace BidArena.Application.Bids.Commands.PlaceBid;

public record PlaceBidCommand(
    Guid AuctionId,
    Guid BidderId,
    decimal Amount,
    decimal? ProxyMaxAmount
) : IRequest<Guid>;