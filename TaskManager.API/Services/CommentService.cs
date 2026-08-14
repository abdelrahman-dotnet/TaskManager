using TaskManager.API.Exceptions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.Comment;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Notification;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Bussiness.Authorization;
using TaskManager.Bussiness.Authorization.ResourceConditions;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;
using TaskManager.Data.Enums;

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
        // G-7 / D-29: @Mention trigger — notification for mentioned members.
        private readonly INotificationService _notificationService;

        public CommentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CommentService> logger,
            IMembershipService membershipService,
            IAuditLogService auditLogService,
            IWorkspaceAuthorizationService authService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _membershipService = membershipService;
            _auditLogService = auditLogService;
            _authService = authService;
            _notificationService = notificationService;
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

            // WORKSPACE PIVOT: resolve the parent project for the audit trail (same
            // scope used by the Authorization pipeline).
            var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("CreateComment failed. Parent project not found. ProjectId: {ProjectId}", task.ProjectId);
                throw new NotFoundException("Comment's project not found.");
            }

            await _unitOfWork.Comments.AddAsync(comment, cancellationToken);
            var newValues = JsonSerializer.Serialize(new { comment.Content, comment.TaskItemId, comment.WorkspaceMemberId });
            // Save first - comment.Id is DB-generated, so it isn't known until after this completes.
            await _unitOfWork.CompleteAsync(cancellationToken);

            var workspaceId = project.WorkspaceId;
            await _auditLogService.LogAsync(currentUserId, "Create Comment", nameof(Comment), comment.Id.ToString(), workspaceId: workspaceId, oldValues: null, newValues: newValues, cancellationToken: cancellationToken);
            // Second save - persists the audit row now that comment.Id exists.
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment created successfully. CommentId: {CommentId}, TaskId: {TaskId}, UserId: {UserId}",
                comment.Id, taskId, currentUserId);

            // G-7 / D-29: store @Mention entities and notify the mentioned members
            // (non-blocking — a mention failure must not roll back the comment).
            await HandleMentionsAsync(comment.Id, dto.Content, project.WorkspaceId, comment.WorkspaceMemberId, currentUserId, cancellationToken);

            return _mapper.Map<CommentReadDto>(comment);
        }

        // Authorization Pipeline: Visibility → Permission (Comments.Update) → Resource Condition
        // (CommentAuthorOnlyCondition — author-only, all roles, no Owner/Admin bypass per S-13) →
        // Operation. Audit trail per BR-AUD-01 (security-relevant mutation).
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

            var oldValues = JsonSerializer.Serialize(new { comment.Content });
            _mapper.Map(dto, comment);
            comment.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Comments.Update(comment);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new { comment.Content });
            await _auditLogService.LogAsync(currentUserId, "Update Comment", nameof(Comment), comment.Id.ToString(), workspaceId: project.WorkspaceId, oldValues: oldValues, newValues: newValues, cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment updated successfully. CommentId: {CommentId}, UserId: {UserId}", id, currentUserId);

            // G-7 / D-29: store @Mention entities for the updated content and notify
            // the newly mentioned members (non-blocking). The comment author is the
            // only one who may update (CommentAuthorOnlyCondition), so editing the
            // comment re-derives its mentions from the new content.
            await HandleMentionsAsync(comment.Id, comment.Content, project.WorkspaceId, comment.WorkspaceMemberId, currentUserId, cancellationToken);

            return _mapper.Map<CommentReadDto>(comment);
        }

        // Authorization Pipeline: Visibility → Permission (Comments.Delete) → Resource Condition
        // (CommentAuthorOnlyCondition — author-only, all roles, no Owner/Admin bypass per S-13) →
        // Operation (soft delete). Audit trail per BR-AUD-01 (security-relevant mutation).
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
            var oldValues = JsonSerializer.Serialize(new { comment.Content, comment.TaskItemId, comment.WorkspaceMemberId });
            _unitOfWork.Comments.Delete(comment);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(currentUserId, "Delete Comment", nameof(Comment), comment.Id.ToString(), workspaceId: project.WorkspaceId, oldValues: oldValues, newValues: null, cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Comment deleted successfully. CommentId: {CommentId}, UserId: {UserId}", id, currentUserId);
        }

        // G-7 / D-29 @MENTION HANDLING: parses @username mentions from the comment
        // content, validates each mentioned user is an ACTIVE member of the
        // workspace (same scope as the comment), stores CommentMention rows, and
        // raises a Mentioned notification for each. Failures are logged but never
        // propagate — the comment itself is the source of truth.
        private async Task HandleMentionsAsync(long commentId, string content, long workspaceId, long commenterMemberId, string currentUserId, CancellationToken cancellationToken)
        {
            try
            {
                var usernames = ParseMentions(content);
                if (usernames.Count == 0)
                    return;

                var mentionedMemberIds = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                    .Where(wm => wm.WorkspaceId == workspaceId
                                 && wm.Status == WorkspaceMemberStatus.Active
                                 && usernames.Contains(wm.User.UserName))
                    .Select(wm => wm.Id)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                foreach (var mentionedMemberId in mentionedMemberIds)
                {
                    // Skip self-mentions silently (mentioning yourself is a no-op).
                    if (mentionedMemberId == commenterMemberId)
                        continue;

                    var mention = new CommentMention
                    {
                        CommentId = commentId,
                        MentionedWorkspaceMemberId = mentionedMemberId,
                        MentionedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.CommentMentions.AddAsync(mention, cancellationToken);
                }
                await _unitOfWork.CompleteAsync(cancellationToken);

                // Notify each newly mentioned member (D-29: NotificationType.Mentioned).
                var notified = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                    .Where(wm => mentionedMemberIds.Contains(wm.Id))
                    .Select(wm => new { wm.Id, wm.UserId })
                    .ToListAsync(cancellationToken);

                foreach (var target in notified)
                {
                    try
                    {
                        await _notificationService.CreateAsync(
                            new NotificationCreateDto
                            {
                                WorkspaceId = workspaceId,
                                UserId = target.UserId,
                                Title = "You were mentioned",
                                Message = $"You were mentioned in a comment."
                            },
                            currentUserId,
                            NotificationType.Mentioned,
                            commentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "@Mention notification failed (non-blocking). CommentId: {CommentId}, TargetId: {TargetId}", commentId, target.Id);
                    }
                }

                _logger.LogInformation("@Mentions stored for CommentId: {CommentId}. MentionCount: {Count}", commentId, mentionedMemberIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "@Mention handling failed (non-blocking). CommentId: {CommentId}", commentId);
            }
        }

        // Extracts distinct usernames from "@username" patterns in the content.
        // Usernames are limited to 3..64 word characters (letters/digits/underscore).
        private static HashSet<string> ParseMentions(string content)
        {
            var mentions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(content))
                return mentions;

            for (var i = 0; i < content.Length; i++)
            {
                if (content[i] != '@')
                    continue;

                var start = i + 1;
                if (start >= content.Length)
                    continue;

                var end = start;
                while (end < content.Length && (char.IsLetterOrDigit(content[end]) || content[end] == '_'))
                    end++;

                var username = content[start..end];
                if (username.Length >= 3 && username.Length <= 64)
                    mentions.Add(username);

                i = end;
            }

            return mentions;
        }
    }
}
