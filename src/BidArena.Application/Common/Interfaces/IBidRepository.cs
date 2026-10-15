using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface IBidRepository
{
    Task<Bid> AddAsync(Bid bid, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<Bid> bids, CancellationToken cancellationToken);

    // Najveci tajni maksimum (ProxyMaxAmount, a ako ga nema Amount) koji je bidder postavio na aukciji.
    Task<decimal?> GetMaxAmountForBidderAsync(Guid auctionId, Guid bidderId, CancellationToken cancellationToken);

    // Istorija ponuda, najnovije prvo.
    Task<List<Bid>> GetByAuctionIdAsync(Guid auctionId, CancellationToken cancellationToken);
}