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
using TaskManager.Bussiness.Authorization;
using TaskManager.Bussiness.Authorization.ResourceConditions;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;
        private readonly IWorkspaceAuthorizationService _authService;

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<NotificationService> logger, IWorkspaceAuthorizationService authService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _authService = authService;
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
            // WORKSPACE PIVOT: Notification.WorkspaceMemberId (long) replaces the old string UserId.
            // The recipient's WorkspaceMember is resolved WITHIN the target workspace (dto.WorkspaceId)
            // instead of scanning for membership in any project.
            var recipientMember = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.WorkspaceId == dto.WorkspaceId && wm.UserId == dto.UserId)
                .OrderBy(wm => wm.Id)
                .FirstOrDefaultAsync();
            if (recipientMember == null)
                throw new BadRequestException("The notification recipient is not a member of the target workspace.");
            var notification = _mapper.Map<Notification>(dto);
            notification.WorkspaceMemberId = recipientMember.Id;

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Notification created successfully. NotificationId: {NotificationId}, UserId: {UserId}",
                notification.Id, notification.WorkspaceMemberId);

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

            var readResult = await _authService.AuthorizeAsync(
                notification.WorkspaceMember.WorkspaceId,
                currentUserId,
                Permissions.WorkspaceView,
                new NotificationRecipientOnlyCondition(notification));
            if (!readResult.Succeeded)
            {
                _logger.LogWarning("MarkAsRead forbidden. UserId: {UserId} tried to read NotificationId: {NotificationId}", currentUserId, id);
                throw readResult.ToAuthorizationException();
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

            var deleteResult = await _authService.AuthorizeAsync(
                notification.WorkspaceMember.WorkspaceId,
                currentUserId,
                Permissions.WorkspaceView,
                new NotificationRecipientOnlyCondition(notification));
            if (!deleteResult.Succeeded)
            {
                _logger.LogWarning("DeleteNotification forbidden. UserId: {UserId} tried to delete NotificationId: {NotificationId}", currentUserId, id);
                throw deleteResult.ToAuthorizationException();
            }

            _unitOfWork.Notifications.Delete(notification);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Notification deleted successfully. NotificationId: {NotificationId}", id);
        }
    }
}
