using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Shipments.Queries.GetMyShipments;

public class GetMyShipmentsQueryHandler : IRequestHandler<GetMyShipmentsQuery, List<ShipmentDto>>
{
    private readonly IShipmentRepository _shipmentRepository;

    public GetMyShipmentsQueryHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<List<ShipmentDto>> Handle(GetMyShipmentsQuery request, CancellationToken cancellationToken)
    {
        var shipments = await _shipmentRepository.GetForUserAsync(request.UserId, cancellationToken);

        return shipments
            .Select(s => new ShipmentDto(
                s.Shipment.Id,
                s.Shipment.AuctionId,
                s.ItemTitle,
                s.SellerId == request.UserId ? "Seller" : "Buyer",
                s.Shipment.TrackingNumber,
                s.Shipment.DueDate,
                s.Shipment.ShippedAt,
                s.Shipment.DeliveredAt,
                s.AuctionStatus.ToString(),
                s.Rated,
                s.SellerId == request.UserId ? s.BuyerId : s.SellerId))
            .ToList();
    }
}
