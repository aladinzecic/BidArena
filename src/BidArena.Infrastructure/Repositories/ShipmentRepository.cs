using BidArena.Application.Common.Interfaces;
using BidArena.Application.Shipments.Models;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly ApplicationDbContext _context;

    public ShipmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Pesimisticko zakljucavanje: mora unutar aktivne transakcije.
    public async Task<Shipment?> GetByAuctionIdForUpdateAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        var result = await _context.Shipments
            .FromSqlInterpolated($"SELECT * FROM \"Shipments\" WHERE \"AuctionId\" = {auctionId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return result.FirstOrDefault();
    }

    public async Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        // Entitet je tracked, EF sam detektuje izmene.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetOverdueUnshippedAuctionIdsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        return await (
            from s in _context.Shipments.AsNoTracking()
            join a in _context.Auctions.AsNoTracking() on s.AuctionId equals a.Id
            where a.Status == AuctionStatus.Ended && s.ShippedAt == null && s.DueDate <= utcNow
            orderby s.DueDate
            select a.Id)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    // Pošiljke u kojima je korisnik prodavac (vlasnik predmeta) ili kupac (pobednik aukcije).
    public async Task<List<ShipmentDetails>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await (
            from s in _context.Shipments.AsNoTracking()
            join a in _context.Auctions.AsNoTracking() on s.AuctionId equals a.Id
            join i in _context.Items.AsNoTracking() on a.ItemId equals i.Id
            where i.SellerId == userId || a.CurrentWinnerId == userId
            orderby s.DueDate descending
            select new
            {
                Shipment = s,
                i.SellerId,
                BuyerId = a.CurrentWinnerId,
                i.Title,
                AuctionStatus = a.Status,
                Rated = _context.Ratings.Any(x => x.AuctionId == a.Id && x.RaterId == userId)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ShipmentDetails(r.Shipment, r.SellerId, r.BuyerId, r.Title, r.AuctionStatus, r.Rated))
            .ToList();
    }
}
