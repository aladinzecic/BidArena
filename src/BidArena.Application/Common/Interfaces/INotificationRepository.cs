using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid userId, NotificationType type, Guid auctionId, CancellationToken cancellationToken);
    Task<List<Notification>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task MarkAsReadAsync(Notification notification, CancellationToken cancellationToken);
}
