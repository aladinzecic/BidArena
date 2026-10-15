using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface IDisputeRepository
{
    Task AddAsync(Dispute dispute, CancellationToken cancellationToken);
    Task<Dispute?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Dispute?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task AddPhotoAsync(DisputePhoto photo, CancellationToken cancellationToken);
    Task UpdateAsync(Dispute dispute, CancellationToken cancellationToken);
    Task<List<Dispute>> GetByStatusAsync(DisputeStatus? status, CancellationToken cancellationToken);
    Task<List<Dispute>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
}
