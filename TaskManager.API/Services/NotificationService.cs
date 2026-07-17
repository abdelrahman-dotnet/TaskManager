using TaskManager.API.Exceptions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Notification;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<NotificationReadDto>> GetAllAsync(NotificationQueryParams queryParams, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Notifications.GetAllQuery().AsNoTracking();

            query = query.ApplyFiltering(queryParams, NotificationFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search;
                query = query.Where(n => n.Title.Contains(search));
            }

            // NOTE: was missing the tie-breaker used everywhere else (x => x.Id), which made
            // Skip/Take non-deterministic whenever the client sends no explicit sort.
            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.Notifications, x => x.Id);

            var projected = query.ProjectTo<NotificationReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Notifications retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<NotificationReadDto> CreateAsync(NotificationCreateDto dto)
        {
            var notification = _mapper.Map<Notification>(dto);
            notification.IsRead = false;

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Notification created successfully. NotificationId: {NotificationId}, UserId: {UserId}",
                notification.Id, notification.UserId);

            return _mapper.Map<NotificationReadDto>(notification);
        }

        public async Task MarkAsReadAsync(long id, string currentUserId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
            if (notification == null)
            {
                _logger.LogWarning("MarkAsRead failed. Notification not found. NotificationId: {NotificationId}", id);
                throw new NotFoundException("Notification not found.");
            }

            if (notification.UserId != currentUserId)
            {
                _logger.LogWarning("MarkAsRead forbidden. UserId: {UserId} tried to read NotificationId: {NotificationId}", currentUserId, id);
                throw new ForbiddenException("You can only read your own notifications.");
            }

            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Notification marked as read. NotificationId: {NotificationId}", id);
        }

        public async Task DeleteAsync(long id, string currentUserId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
            if (notification == null)
            {
                _logger.LogWarning("DeleteNotification failed. Not found. NotificationId: {NotificationId}", id);
                throw new NotFoundException("Notification not found.");
            }

            if (notification.UserId != currentUserId)
            {
                _logger.LogWarning("DeleteNotification forbidden. UserId: {UserId} tried to delete NotificationId: {NotificationId}", currentUserId, id);
                throw new ForbiddenException("You can only delete your own notifications.");
            }

            _unitOfWork.Notifications.Delete(notification);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Notification deleted successfully. NotificationId: {NotificationId}", id);
        }
    }
}