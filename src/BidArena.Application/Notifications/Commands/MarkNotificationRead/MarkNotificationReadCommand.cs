using MediatR;

namespace BidArena.Application.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : IRequest;
