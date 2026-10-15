using MediatR;

namespace BidArena.Application.Auctions.Commands.ActivateAuction;

public record ActivateAuctionCommand(Guid AuctionId, Guid RequesterId) : IRequest;