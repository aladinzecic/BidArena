using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<Payment?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken);
    Task<List<Payment>> GetByWinnerIdAsync(Guid winnerId, CancellationToken cancellationToken);
    Task<Payment?> GetByAuctionIdForUpdateAsync(Guid auctionId, CancellationToken cancellationToken);
    Task<Payment?> GetByAuctionIdAsync(Guid auctionId, CancellationToken cancellationToken);
    Task<List<Guid>> GetOverduePendingIdsAsync(DateTime utcNow, CancellationToken cancellationToken);
}