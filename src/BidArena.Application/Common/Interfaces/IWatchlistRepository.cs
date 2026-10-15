using BidArena.Application.Auctions.Models;
using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface IWatchlistRepository
{
    Task AddAsync(WatchlistItem item, CancellationToken cancellationToken);
    Task RemoveAsync(Guid userId, Guid auctionId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid userId, Guid auctionId, CancellationToken cancellationToken);
    Task<List<Guid>> GetWatcherIdsAsync(Guid auctionId, CancellationToken cancellationToken);
    Task<List<AuctionWithItem>> GetMyWatchlistAsync(Guid userId, CancellationToken cancellationToken);
}
