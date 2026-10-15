using MediatR;

namespace BidArena.Application.Auctions.Commands.NotifyEndingSoon;

public record NotifyAuctionEndingSoonCommand(Guid AuctionId) : IRequest;
