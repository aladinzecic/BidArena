using MediatR;

namespace BidArena.Application.Bids.Queries.GetBidHistory;

public record GetBidHistoryQuery(Guid AuctionId) : IRequest<List<BidDto>>;