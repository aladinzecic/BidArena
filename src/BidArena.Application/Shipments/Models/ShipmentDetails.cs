using BidArena.Domain.Entities;

namespace BidArena.Application.Shipments.Models;

// Rated: da li je korisnik koji gleda pošiljku već ocenio drugu stranu u ovoj aukciji.
public record ShipmentDetails(
    Shipment Shipment, Guid SellerId, Guid? BuyerId, string ItemTitle, AuctionStatus AuctionStatus, bool Rated);
