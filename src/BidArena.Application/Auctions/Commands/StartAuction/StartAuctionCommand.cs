using MediatR;

namespace BidArena.Application.Auctions.Commands.StartAuction;

// Sistemska komanda (poziva je worker): Scheduled -> Active kada nastupi StartTime.
public record StartAuctionCommand(Guid AuctionId) : IRequest;
