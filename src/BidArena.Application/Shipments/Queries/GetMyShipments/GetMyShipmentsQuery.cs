using MediatR;

namespace BidArena.Application.Shipments.Queries.GetMyShipments;

public record GetMyShipmentsQuery(Guid UserId) : IRequest<List<ShipmentDto>>;
