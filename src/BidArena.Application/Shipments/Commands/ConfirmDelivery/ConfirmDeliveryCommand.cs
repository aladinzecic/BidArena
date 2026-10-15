using MediatR;

namespace BidArena.Application.Shipments.Commands.ConfirmDelivery;

public record ConfirmDeliveryCommand(Guid AuctionId, Guid BuyerId) : IRequest;
