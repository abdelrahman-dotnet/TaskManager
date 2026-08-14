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
using TaskManager.Bussiness.Authorization;
using TaskManager.Bussiness.Authorization.ResourceConditions;
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
        private readonly IWorkspaceAuthorizationService _authService;

        public CommentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CommentService> logger,
            IMembershipService membershipService,
            IAuditLogService auditLogService,
            IWorkspaceAuthorizationService authService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _membershipService = membershipService;
            _auditLogService = auditLogService;
            _authService = authService;
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
                    .Where(pm => pm.WorkspaceMember.UserId == currentUserId)
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
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("CreateComment failed. Task not found. TaskId: {TaskId}", taskId);
                throw new NotFoundException("Task not found.");
            }

            // WORKSPACE PIVOT: the FK is now a member id; resolve the caller's membership
            // within the task's project (same workspace scope used by the Authorization pipeline).
            var callerMemberId = await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == task.ProjectId && pm.WorkspaceMember.UserId == currentUserId)
                .Select(pm => pm.WorkspaceMemberId)
                .FirstOrDefaultAsync(cancellationToken);
            if (callerMemberId == 0)
                throw new ForbiddenException("You are not a member of this task's project.");

            var canAccess = await _membershipService.CanAccessTaskAsync(taskId, currentUserId, cancellationToken);
            if (!canAccess)
            {
                _logger.LogWarning("CreateComment forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, taskId);
                throw new ForbiddenException("You are not a member of this task's project.");
            }

            var comment = _mapper.Map<Comment>(dto);
            comment.TaskItemId = taskId;
            comment.WorkspaceMemberId = callerMemberId;

            await _unitOfWork.Comments.AddAsync(comment, cancellationToken);
            var newValues = JsonSerializer.Serialize(new { comment.Content, comment.TaskItemId, comment.WorkspaceMemberId });
            // Save first - comment.Id is DB-generated, so it isn't known until after this completes.
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(currentUserId, "Create Comment", nameof(Comment), comment.Id.ToString(), null, newValues, cancellationToken);
            // Second save - persists the audit row now that comment.Id exists.
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment created successfully. CommentId: {CommentId}, TaskId: {TaskId}, UserId: {UserId}",
                comment.Id, taskId, currentUserId);

            return _mapper.Map<CommentReadDto>(comment);
        }

        // Authorization Pipeline: Visibility → Permission (Comments.Update) → Resource Condition
        // (CommentAuthorOnlyCondition — author-only, all roles, no Owner/Admin bypass per S-13) →
        // Operation. BR-AUD-01 excludes comment operations from the audit log.
        public async Task<CommentReadDto> UpdateAsync(long id, CommentUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(id, cancellationToken);
            if (comment == null)
            {
                _logger.LogWarning("UpdateComment failed. Comment not found. CommentId: {CommentId}", id);
                throw new NotFoundException("Comment not found.");
            }

            // WORKSPACE PIVOT: resolve the workspace through comment → task → project, the same
            // scope used by the Authorization pipeline.
            var task = await _unitOfWork.Tasks.GetByIdAsync(comment.TaskItemId, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("UpdateComment failed. Parent task not found. TaskId: {TaskId}", comment.TaskItemId);
                throw new NotFoundException("Comment's task not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("UpdateComment failed. Parent project not found. ProjectId: {ProjectId}", task.ProjectId);
                throw new NotFoundException("Comment's project not found.");
            }

            // === Authorization Pipeline (المراحل 1+2+3) ===
            var authResult = await _authService.AuthorizeAsync(
                project.WorkspaceId,
                currentUserId,
                Permissions.CommentsUpdate,
                new CommentAuthorOnlyCondition(comment));

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "UpdateComment pipeline failed. Reason: {Reason}, UserId: {UserId}, CommentId: {CommentId}",
                    authResult.FailureReason, currentUserId, id);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            _mapper.Map(dto, comment);
            comment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Comments.Update(comment);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment updated successfully. CommentId: {CommentId}", id);
            return _mapper.Map<CommentReadDto>(comment);
        }

        // Authorization Pipeline: Visibility → Permission (Comments.Delete) → Resource Condition
        // (CommentAuthorOnlyCondition — author-only, all roles, no Owner/Admin bypass per S-13) →
        // Operation (soft delete). BR-AUD-01 excludes comment operations from the audit log.
        public async Task DeleteAsync(long id, string currentUserId, CancellationToken cancellationToken = default)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(id, cancellationToken);
            if (comment == null)
            {
                _logger.LogWarning("DeleteComment failed. Not found. CommentId: {CommentId}", id);
                throw new NotFoundException("Comment not found.");
            }

            // WORKSPACE PIVOT: resolve the workspace through comment → task → project, the same
            // scope used by the Authorization pipeline.
            var task = await _unitOfWork.Tasks.GetByIdAsync(comment.TaskItemId, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("DeleteComment failed. Parent task not found. TaskId: {TaskId}", comment.TaskItemId);
                throw new NotFoundException("Comment's task not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("DeleteComment failed. Parent project not found. ProjectId: {ProjectId}", task.ProjectId);
                throw new NotFoundException("Comment's project not found.");
            }

            // === Authorization Pipeline (المراحل 1+2+3) ===
            var authResult = await _authService.AuthorizeAsync(
                project.WorkspaceId,
                currentUserId,
                Permissions.CommentsDelete,
                new CommentAuthorOnlyCondition(comment));

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "DeleteComment pipeline failed. Reason: {Reason}, UserId: {UserId}, CommentId: {CommentId}",
                    authResult.FailureReason, currentUserId, id);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            // === المرحلة 5: Operation (Soft Delete) ===
            _unitOfWork.Comments.Delete(comment);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment deleted successfully. CommentId: {CommentId}", id);
        }
    }
}
