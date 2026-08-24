using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Notification;
using TaskManager.API.Helpers;
using TaskManager.Data.Enums;

namespace TaskManager.Business.Services.Interfaces
{
    public interface INotificationService
    {
        Task<PagedResult<NotificationReadDto>> GetAllAsync(NotificationQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default);
        Task<NotificationReadDto> CreateAsync(NotificationCreateDto dto, string currentUserId, NotificationType type = NotificationType.TaskAssigned, long? referenceId = null, string? triggeringPermission = null);
        Task MarkAsReadAsync(long id, string currentUserId);
        Task DeleteAsync(long id, string currentUserId);
    }
}
