using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, NotificationType type, Guid auctionId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .AnyAsync(n => n.UserId == userId && n.Type == type && n.AuctionId == auctionId, cancellationToken);
    }

    public async Task<List<Notification>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task MarkAsReadAsync(Notification notification, CancellationToken cancellationToken)
    {
        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
