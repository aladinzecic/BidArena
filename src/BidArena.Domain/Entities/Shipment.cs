namespace BidArena.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }

    public string? TrackingNumber { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime DueDate { get; set; }
}