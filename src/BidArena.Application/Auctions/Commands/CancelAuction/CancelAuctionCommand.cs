using MediatR;

namespace BidArena.Application.Auctions.Commands.CancelAuction;

public record CancelAuctionCommand(Guid AuctionId, Guid RequesterId) : IRequest;
