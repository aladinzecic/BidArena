using MediatR;

namespace BidArena.Application.Disputes.Commands.TakeDispute;

public record TakeDisputeCommand(Guid DisputeId) : IRequest;
