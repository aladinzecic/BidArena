using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class DisputeRepository : IDisputeRepository
{
    private readonly ApplicationDbContext _context;

    public DisputeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Dispute dispute, CancellationToken cancellationToken)
    {
        _context.Disputes.Add(dispute);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Dispute?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Disputes
            .Include(d => d.Photos)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task AddPhotoAsync(DisputePhoto photo, CancellationToken cancellationToken)
    {
        _context.DisputePhotos.Add(photo);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Pesimisticko zakljucavanje: mora unutar aktivne transakcije.
    public async Task<Dispute?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _context.Disputes
            .FromSqlInterpolated($"SELECT * FROM \"Disputes\" WHERE \"Id\" = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return result.FirstOrDefault();
    }

    public async Task UpdateAsync(Dispute dispute, CancellationToken cancellationToken)
    {
        // Entitet je tracked, EF sam detektuje izmene.
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Najstariji prvi: administrator obrađuje red čekanja redom.
    public async Task<List<Dispute>> GetByStatusAsync(DisputeStatus? status, CancellationToken cancellationToken)
    {
        var query = _context.Disputes.AsNoTracking().Include(d => d.Photos).AsQueryable();

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        return await query
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    // Sporovi koje je korisnik otvorio ili u kojima je druga strana (prodavac / pobednik).
    public async Task<List<Dispute>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
            from d in _context.Disputes.AsNoTracking().Include(x => x.Photos)
            join a in _context.Auctions.AsNoTracking() on d.AuctionId equals a.Id
            join i in _context.Items.AsNoTracking() on a.ItemId equals i.Id
            where d.OpenedByUserId == userId || a.CurrentWinnerId == userId || i.SellerId == userId
            orderby d.CreatedAt descending
            select d)
            .ToListAsync(cancellationToken);
    }
}
