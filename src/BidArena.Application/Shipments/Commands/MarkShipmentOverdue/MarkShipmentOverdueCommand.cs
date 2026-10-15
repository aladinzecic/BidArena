using MediatR;

namespace BidArena.Application.Shipments.Commands.MarkShipmentOverdue;

public record MarkShipmentOverdueCommand(Guid AuctionId) : IRequest;
