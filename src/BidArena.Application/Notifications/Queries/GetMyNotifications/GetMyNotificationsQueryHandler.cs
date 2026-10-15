using AutoMapper;
using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, List<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IMapper _mapper;

    public GetMyNotificationsQueryHandler(INotificationRepository notificationRepository, IMapper mapper)
    {
        _notificationRepository = notificationRepository;
        _mapper = mapper;
    }

    public async Task<List<NotificationDto>> Handle(
        GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await _notificationRepository.GetByUserAsync(request.UserId, cancellationToken);

        return _mapper.Map<List<NotificationDto>>(notifications);
    }
}
