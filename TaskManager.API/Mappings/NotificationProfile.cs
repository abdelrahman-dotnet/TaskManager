using AutoMapper;
using TaskManager.API.DTOs.Notification;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<Notification, NotificationReadDto>();
            CreateMap<NotificationCreateDto, Notification>();
        }
    }
}
