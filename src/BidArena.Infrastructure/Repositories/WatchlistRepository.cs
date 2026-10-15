using BidArena.Application.Auctions.Models;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class WatchlistRepository : IWatchlistRepository
{
    private readonly ApplicationDbContext _context;

    public WatchlistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WatchlistItem item, CancellationToken cancellationToken)
    {
        _context.WatchlistItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid userId, Guid auctionId, CancellationToken cancellationToken)
    {
        var item = await _context.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.AuctionId == auctionId, cancellationToken);

        if (item is null)
            return; // idempotentno - brisanje nepostojeceg ne baca gresku

        _context.WatchlistItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid auctionId, CancellationToken cancellationToken)
    {
        return await _context.WatchlistItems
            .AnyAsync(w => w.UserId == userId && w.AuctionId == auctionId, cancellationToken);
    }

    public async Task<List<Guid>> GetWatcherIdsAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        return await _context.WatchlistItems
            .AsNoTracking()
            .Where(w => w.AuctionId == auctionId)
            .Select(w => w.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuctionWithItem>> GetMyWatchlistAsync(Guid userId, CancellationToken cancellationToken)
    {
        var query =
            from w in _context.WatchlistItems.AsNoTracking()
            join a in _context.Auctions.AsNoTracking() on w.AuctionId equals a.Id
            join i in _context.Items.AsNoTracking() on a.ItemId equals i.Id
            where w.UserId == userId
            orderby w.AddedAt descending
            select new { Auction = a, Item = i };

        var rows = await query
            .Select(x => new
            {
                x.Auction,
                x.Item,
                BidCount = x.Auction.Bids.Count,
                MainPhotoUrl = x.Item.Photos
                    .OrderBy(p => p.OrderIndex)
                    .Select(p => p.Url)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new AuctionWithItem(r.Auction, r.Item, r.BidCount, r.MainPhotoUrl))
            .ToList();
    }
}
