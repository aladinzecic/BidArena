using BidArena.Application.Auctions.Models;
using BidArena.Application.Common.Models;
using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface IAuctionRepository
{
    Task<Auction> AddAsync(Auction auction, CancellationToken cancellationToken);
    Task<Auction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Auction?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<AuctionStatus?> GetStatusAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Auction auction, CancellationToken cancellationToken);
    Task<List<Guid>> GetExpiredActiveIdsAsync(DateTime utcNow, CancellationToken cancellationToken);
    // Aktivne aukcije koje se završavaju u zadatom intervalu (za obaveštenje AuctionEndingSoon).
    Task<List<Guid>> GetEndingSoonIdsAsync(DateTime from, DateTime to, CancellationToken cancellationToken);
    Task<List<Guid>> GetDueScheduledIdsAsync(DateTime utcNow, CancellationToken cancellationToken);
    Task<List<AuctionStatus>> GetStatusesByItemIdAsync(Guid itemId, CancellationToken cancellationToken);
    Task<AuctionWithItem?> GetWithItemAsync(Guid id, CancellationToken cancellationToken);
    Task<List<AuctionWithItem>> GetBySellerAsync(Guid sellerId, CancellationToken cancellationToken);
    Task<PagedResult<AuctionWithItem>> SearchAsync(AuctionSearchCriteria criteria, CancellationToken cancellationToken);
}