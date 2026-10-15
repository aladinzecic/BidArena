using BidArena.Application.Admin.Queries.GetAdminStats;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class AdminStatsRepository : IAdminStatsRepository
{
    private readonly ApplicationDbContext _context;

    public AdminStatsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminStatsDto> GetAsync(CancellationToken cancellationToken)
    {
        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var verifiedUsers = await _context.Users.CountAsync(u => u.IsVerified, cancellationToken);

        var grouped = await _context.Auctions
            .AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // Svi statusi su uvek prisutni u odgovoru, i oni sa nula aukcija.
        var auctionsByStatus = Enum.GetValues<AuctionStatus>()
            .ToDictionary(
                s => s.ToString(),
                s => grouped.FirstOrDefault(g => g.Status == s)?.Count ?? 0);

        var pendingPayments = await _context.Payments
            .CountAsync(p => p.Status == PaymentStatus.Pending, cancellationToken);

        var overduePayments = await _context.Payments
            .CountAsync(p => p.Status == PaymentStatus.Overdue, cancellationToken);

        var turnover = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var commission = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .SumAsync(p => (decimal?)p.Commission, cancellationToken) ?? 0m;

        var openDisputes = await _context.Disputes
            .CountAsync(d => d.Status != DisputeStatus.Resolved, cancellationToken);

        return new AdminStatsDto(
            totalUsers,
            verifiedUsers,
            auctionsByStatus,
            pendingPayments,
            overduePayments,
            turnover,
            commission,
            openDisputes);
    }
}
