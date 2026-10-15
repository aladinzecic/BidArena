namespace BidArena.Application.Shipments.Queries.GetMyShipments;

public record ShipmentDto(
    Guid Id,
    Guid AuctionId,
    string ItemTitle,
    string Role,
    string? TrackingNumber,
    DateTime DueDate,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    string AuctionStatus,
    bool Rated,
    Guid? CounterpartId
);
