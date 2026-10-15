using MediatR;

namespace BidArena.Application.Shipments.Commands.MarkShipped;

public record MarkShippedCommand(Guid AuctionId, Guid SellerId, string TrackingNumber) : IRequest;
