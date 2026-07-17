using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Notification;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface INotificationService
    {
        Task<PagedResult<NotificationReadDto>> GetAllAsync(NotificationQueryParams queryParams, CancellationToken cancellationToken = default);
        Task<NotificationReadDto> CreateAsync(NotificationCreateDto dto);
        Task MarkAsReadAsync(long id, string currentUserId);
        Task DeleteAsync(long id, string currentUserId);
    }
}