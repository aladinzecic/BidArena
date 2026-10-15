using MediatR;

namespace BidArena.Application.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(Guid UserId) : IRequest<List<NotificationDto>>;
