using AutoMapper;
using BidArena.Application.Notifications.Queries.GetMyNotifications;
using BidArena.Domain.Entities;

namespace BidArena.Application.Notifications.Mappings;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationDto>()
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));
    }
}
