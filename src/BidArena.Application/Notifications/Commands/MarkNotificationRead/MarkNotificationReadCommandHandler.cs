using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Notifications.Commands.MarkNotificationRead;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly INotificationRepository _notificationRepository;

    public MarkNotificationReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Notifikacija {request.NotificationId} ne postoji");

        if (notification.UserId != request.UserId)
            throw new ForbiddenAccessException("Ova notifikacija ne pripada prijavljenom korisniku");

        if (notification.IsRead)
            return; // idempotentno

        await _notificationRepository.MarkAsReadAsync(notification, cancellationToken);
    }
}
