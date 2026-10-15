using MediatR;

namespace BidArena.Application.Disputes.Commands.OpenDispute;

public record OpenDisputeCommand(Guid AuctionId, Guid UserId, string Reason) : IRequest<Guid>;
