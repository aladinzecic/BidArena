using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BidArena.Application.Auctions.Models;
using BidArena.Application.Common.Models;
namespace BidArena.Infrastructure.Repositories;

public class AuctionRepository : IAuctionRepository
{
    private readonly ApplicationDbContext _context;

    public AuctionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Auction> AddAsync(Auction auction, CancellationToken cancellationToken)
    {
        _context.Auctions.Add(auction);
        await _context.SaveChangesAsync(cancellationToken);
        return auction;
    }

    public async Task<Auction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Auctions
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    // Pesimisticko zakljucavanje: SELECT ... FOR UPDATE.
    // Mora se pozivati unutar aktivne transakcije; lock traje do COMMIT/ROLLBACK.
    // "xmin" se navodi eksplicitno jer je Version mapiran na tu sistemsku kolonu,
    // a SELECT * sistemske kolone ne vraca.
    public async Task<Auction?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _context.Auctions
            .FromSqlInterpolated($"SELECT *, xmin FROM \"Auctions\" WHERE \"Id\" = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return result.FirstOrDefault();
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Auctions.AnyAsync(a => a.Id == id, cancellationToken);
    }

    // Laki upit: cita samo status, bez ucitavanja celog entiteta i bez trackinga.
    public async Task<AuctionStatus?> GetStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Auctions
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => (AuctionStatus?)a.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(Auction auction, CancellationToken cancellationToken)
    {
        // Entitet je vec tracked, pa EF sam detektuje izmene.
        await _context.SaveChangesAsync(cancellationToken);
    }
    // Paketno, najviše 100 po ciklusu, da jedan ciklus ne traje predugo.
public async Task<List<Guid>> GetExpiredActiveIdsAsync(DateTime utcNow, CancellationToken cancellationToken)
{
    return await _context.Auctions
        .AsNoTracking()
        .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= utcNow)
        .OrderBy(a => a.EndTime)
        .Select(a => a.Id)
        .Take(100)
        .ToListAsync(cancellationToken);
}

    public async Task<List<Guid>> GetEndingSoonIdsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        return await _context.Auctions
            .AsNoTracking()
            .Where(a => a.Status == AuctionStatus.Active && a.EndTime > from && a.EndTime <= to)
            .OrderBy(a => a.EndTime)
            .Select(a => a.Id)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    // Zakazane aukcije kojima je nastupilo vreme pocetka (paketno, najvise 100 po ciklusu).
    public async Task<List<Guid>> GetDueScheduledIdsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        return await _context.Auctions
            .AsNoTracking()
            .Where(a => a.Status == AuctionStatus.Scheduled && a.StartTime <= utcNow)
            .OrderBy(a => a.StartTime)
            .Select(a => a.Id)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuctionStatus>> GetStatusesByItemIdAsync(Guid itemId, CancellationToken cancellationToken)
    {
        return await _context.Auctions
            .AsNoTracking()
            .Where(a => a.ItemId == itemId)
            .Select(a => a.Status)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuctionWithItem?> GetWithItemAsync(Guid id, CancellationToken cancellationToken)
    {
        var auction = await _context.Auctions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (auction is null)
            return null;

        var item = await _context.Items
            .AsNoTracking()
            .Include(i => i.Photos)
            .FirstOrDefaultAsync(i => i.Id == auction.ItemId, cancellationToken);

        if (item is null)
            return null;

        var bidCount = await _context.Bids
            .CountAsync(b => b.AuctionId == id, cancellationToken);

        var mainPhoto = item.Photos
            .OrderBy(p => p.OrderIndex)
            .Select(p => p.Url)
            .FirstOrDefault();

        return new AuctionWithItem(auction, item, bidCount, mainPhoto);
    }

    // Sve aukcije prodavca (uključujući Draft), najnovije prve.
    public async Task<List<AuctionWithItem>> GetBySellerAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var rows = await (
            from a in _context.Auctions.AsNoTracking()
            join i in _context.Items.AsNoTracking() on a.ItemId equals i.Id
            where i.SellerId == sellerId
            orderby a.StartTime descending
            select new
            {
                Auction = a,
                Item = i,
                BidCount = a.Bids.Count,
                MainPhotoUrl = i.Photos
                    .OrderBy(p => p.OrderIndex)
                    .Select(p => p.Url)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new AuctionWithItem(r.Auction, r.Item, r.BidCount, r.MainPhotoUrl))
            .ToList();
    }

    public async Task<PagedResult<AuctionWithItem>> SearchAsync(
        AuctionSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query =
            from a in _context.Auctions.AsNoTracking()
            join i in _context.Items.AsNoTracking() on a.ItemId equals i.Id
            where a.Status == criteria.Status && a.Status != AuctionStatus.Draft
            select new { Auction = a, Item = i };

        if (criteria.Category.HasValue)
        {
            var category = criteria.Category.Value;
            query = query.Where(x => x.Item.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var pattern = $"%{criteria.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Item.Title, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(x => x.Auction.EndTime)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
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

        var items = rows
            .Select(r => new AuctionWithItem(r.Auction, r.Item, r.BidCount, r.MainPhotoUrl))
            .ToList();

        return new PagedResult<AuctionWithItem>(items, totalCount, criteria.Page, criteria.PageSize);
    }
}