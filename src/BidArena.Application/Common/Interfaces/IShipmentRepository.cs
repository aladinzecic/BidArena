using BidArena.Application.Shipments.Models;
using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface IShipmentRepository
{
    Task AddAsync(Shipment shipment, CancellationToken cancellationToken);
    Task<Shipment?> GetByAuctionIdForUpdateAsync(Guid auctionId, CancellationToken cancellationToken);
    Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken);
    // Aukcije (Ended) čiji je rok slanja istekao, a prodavac nije poslao predmet.
    Task<List<Guid>> GetOverdueUnshippedAuctionIdsAsync(DateTime utcNow, CancellationToken cancellationToken);
    Task<List<ShipmentDetails>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
}
