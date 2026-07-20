using TaskManager.API.Exceptions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.Comment;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CommentService> _logger;
        private readonly IMembershipService _membershipService;
        private readonly IAuditLogService _auditLogService;

        public CommentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CommentService> logger,
            IMembershipService membershipService,
            IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _membershipService = membershipService;
            _auditLogService = auditLogService;
        }

        // MEMBERSHIP: Comment has no ProjectId of its own (only TaskItemId), so this is the
        // same two-level subquery pattern as AttachmentService.GetAllAsync: ProjectMembers ->
        // accessible ProjectIds -> Tasks in those projects -> accessible TaskIds -> Comments on
        // those tasks. Still one query, not fetch-then-filter.
        public async Task<PagedResult<CommentReadDto>> GetAllAsync(CommentQueryParams queryParams, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Comments.GetAllQuery().AsNoTracking();

            if (!canManageAny)
            {
                var memberProjectIds = _unitOfWork.ProjectMembers.GetAllQuery()
                    .Where(pm => pm.UserId == currentUserId)
                    .Select(pm => pm.ProjectId);

                var accessibleTaskIds = _unitOfWork.Tasks.GetAllQuery()
                    .Where(t => memberProjectIds.Contains(t.ProjectId))
                    .Select(t => t.Id);

                query = query.Where(c => accessibleTaskIds.Contains(c.TaskItemId));
            }

            query = query.ApplyFiltering(queryParams, CommentFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search;
                query = query.Where(c => c.Content.Contains(search));
            }

            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.Comments, x => x.Id);

            var projected = query.ProjectTo<CommentReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Comments retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<IEnumerable<CommentReadDto>> GetByTaskIdAsync(long taskId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var canAccess = await _membershipService.CanAccessTaskAsync(taskId, currentUserId, cancellationToken);
            if (!canAccess)
            {
                _logger.LogWarning("GetCommentsByTask forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, taskId);
                throw new ForbiddenException("You are not a member of this task's project.");
            }

            var comments = await _unitOfWork.Comments.GetAllQuery()
                .AsNoTracking()
                .Where(c => c.TaskItemId == taskId)
                .OrderByDescending(c => c.CreatedAt)
                .ProjectTo<CommentReadDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            if (!comments.Any())
                _logger.LogWarning("No comments found for TaskId: {TaskId}", taskId);

            return comments;
        }

        public async Task<CommentReadDto> CreateAsync(long taskId, CommentCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var taskExists = await _unitOfWork.Tasks.ExistsAsync(t => t.Id == taskId, cancellationToken);
            if (!taskExists)
            {
                _logger.LogWarning("CreateComment failed. Task not found. TaskId: {TaskId}", taskId);
                throw new NotFoundException("Task not found.");
            }

            var canAccess = await _membershipService.CanAccessTaskAsync(taskId, currentUserId, cancellationToken);
            if (!canAccess)
            {
                _logger.LogWarning("CreateComment forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, taskId);
                throw new ForbiddenException("You are not a member of this task's project.");
            }

            var comment = _mapper.Map<Comment>(dto);
            comment.TaskItemId = taskId;
            comment.UserId = currentUserId;

            await _unitOfWork.Comments.AddAsync(comment, cancellationToken);
            var newValues = JsonSerializer.Serialize(new { comment.Content, comment.TaskItemId, comment.UserId });
            // Save first - comment.Id is DB-generated, so it isn't known until after this completes.
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(currentUserId, "Create Comment", nameof(Comment), comment.Id.ToString(), null, newValues, cancellationToken);
            // Second save - persists the audit row now that comment.Id exists.
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment created successfully. CommentId: {CommentId}, TaskId: {TaskId}, UserId: {UserId}",
                comment.Id, taskId, currentUserId);

            return _mapper.Map<CommentReadDto>(comment);
        }

        public async Task<CommentReadDto> UpdateAsync(long id, CommentUpdateDto dto, bool canManageAny, string currentUserId, CancellationToken cancellationToken = default)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(id, cancellationToken);
            if (comment == null)
            {
                _logger.LogWarning("UpdateComment failed. Comment not found. CommentId: {CommentId}", id);
                throw new NotFoundException("Comment not found.");
            }

            var canAccessTask = await _membershipService.CanAccessTaskAsync(comment.TaskItemId, currentUserId, cancellationToken);
            if (!canAccessTask)
            {
                _logger.LogWarning("UpdateComment forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, comment.TaskItemId);
                throw new ForbiddenException("You are not a member of this task's project.");
            }

            if (!canManageAny && comment.UserId != currentUserId)
            {
                _logger.LogWarning("UpdateComment forbidden. UserId: {UserId} tried to edit CommentId: {CommentId}", currentUserId, id);
                throw new ForbiddenException("You can only edit your own comments.");
            }

            var oldValues = JsonSerializer.Serialize(new { comment.Content });

            _mapper.Map(dto, comment);
            comment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Comments.Update(comment);

            var newValues = JsonSerializer.Serialize(new { comment.Content });
            await _auditLogService.LogAsync(currentUserId, "Update Comment", nameof(Comment), id.ToString(), oldValues, newValues, cancellationToken);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment updated successfully. CommentId: {CommentId}", id);
            return _mapper.Map<CommentReadDto>(comment);
        }

        public async Task DeleteAsync(long id, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(id, cancellationToken);
            if (comment == null)
            {
                _logger.LogWarning("DeleteComment failed. Not found. CommentId: {CommentId}", id);
                throw new NotFoundException("Comment not found.");
            }

            var canAccessTask = await _membershipService.CanAccessTaskAsync(comment.TaskItemId, currentUserId, cancellationToken);
            if (!canAccessTask)
            {
                _logger.LogWarning("DeleteComment forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, comment.TaskItemId);
                throw new ForbiddenException("You are not a member of this task's project.");
            }

            if (!canManageAny && comment.UserId != currentUserId)
            {
                _logger.LogWarning("DeleteComment forbidden. UserId: {UserId} tried to delete CommentId: {CommentId}", currentUserId, id);
                throw new ForbiddenException("You can only delete your own comments.");
            }

            var oldValues = JsonSerializer.Serialize(new { comment.Content, comment.TaskItemId, comment.UserId });

            _unitOfWork.Comments.Delete(comment);

            await _auditLogService.LogAsync(currentUserId, "Delete Comment", nameof(Comment), id.ToString(), oldValues, null, cancellationToken);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment deleted successfully. CommentId: {CommentId}", id);
        }
    }
}
