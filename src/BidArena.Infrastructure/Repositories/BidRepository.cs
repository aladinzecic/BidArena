using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class BidRepository : IBidRepository
{
    private readonly ApplicationDbContext _context;

    public BidRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Bid> AddAsync(Bid bid, CancellationToken cancellationToken)
    {
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync(cancellationToken);
        return bid;
    }

    public async Task AddRangeAsync(IEnumerable<Bid> bids, CancellationToken cancellationToken)
    {
        _context.Bids.AddRange(bids);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal?> GetMaxAmountForBidderAsync(Guid auctionId, Guid bidderId, CancellationToken cancellationToken)
    {
        return await _context.Bids
            .Where(b => b.AuctionId == auctionId && b.BidderId == bidderId)
            .MaxAsync(b => (decimal?)(b.ProxyMaxAmount ?? b.Amount), cancellationToken);
    }

    public async Task<List<Bid>> GetByAuctionIdAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        return await _context.Bids
            .AsNoTracking()
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Amount)
            .ToListAsync(cancellationToken);
    }
}