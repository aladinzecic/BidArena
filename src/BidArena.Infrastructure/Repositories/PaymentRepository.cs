using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Pesimisticko zakljucavanje, isto kao kod aukcija: mora unutar aktivne transakcije.
    // Sprecava da plaćanje i job za Overdue istovremeno menjaju isti red.
    public async Task<Payment?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _context.Payments
            .FromSqlInterpolated($"SELECT * FROM \"Payments\" WHERE \"Id\" = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return result.FirstOrDefault();
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken)
    {
        // Entitet je tracked, EF sam detektuje izmene.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Payment>> GetByWinnerIdAsync(Guid winnerId, CancellationToken cancellationToken)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.WinnerId == winnerId)
            .OrderByDescending(p => p.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByAuctionIdForUpdateAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        var result = await _context.Payments
            .FromSqlInterpolated($"SELECT * FROM \"Payments\" WHERE \"AuctionId\" = {auctionId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return result.FirstOrDefault();
    }

    public async Task<Payment?> GetByAuctionIdAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        return await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AuctionId == auctionId, cancellationToken);
    }

    // Najviše 100 po ciklusu.
    public async Task<List<Guid>> GetOverduePendingIdsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Pending && p.DueDate <= utcNow)
            .OrderBy(p => p.DueDate)
            .Select(p => p.Id)
            .Take(100)
            .ToListAsync(cancellationToken);
    }
}