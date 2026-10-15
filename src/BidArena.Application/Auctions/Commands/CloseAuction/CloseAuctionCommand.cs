using MediatR;

namespace BidArena.Application.Auctions.Commands.CloseAuction;

public record CloseAuctionCommand(Guid AuctionId) : IRequest;