using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.Attachment;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Exceptions;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Bussiness.Authorization;
using TaskManager.Bussiness.Authorization.ResourceConditions;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AttachmentService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IMembershipService _membershipService;
        private readonly IWorkspaceAuthorizationService _authService;

        public AttachmentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AttachmentService> logger,
            IAuditLogService auditLogService,
            IMembershipService membershipService,
            IWorkspaceAuthorizationService authService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _auditLogService = auditLogService;
            _membershipService = membershipService;
            _authService = authService;
        }

        // MEMBERSHIP: Attachment has no ProjectId of its own (only TaskItemId), so the filter
        // is a two-level subquery: ProjectMembers -> accessible ProjectIds -> Tasks in those
        // projects -> accessible TaskIds -> Attachments on those tasks. Still a single query
        // (EF Core translates the nested Contains() calls into nested SQL IN(...)), not
        // fetch-then-filter in memory.
        public async Task<PagedResult<AttachmentReadDto>> GetAllAsync(AttachmentQueryParams queryParams, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Attachments.GetAllQuery().AsNoTracking();

            if (!canManageAny)
            {
                var memberProjectIds = _unitOfWork.ProjectMembers.GetAllQuery()
                    .Where(pm => pm.WorkspaceMember.UserId == currentUserId)
                    .Select(pm => pm.ProjectId);

                var accessibleTaskIds = _unitOfWork.Tasks.GetAllQuery()
                    .Where(t => memberProjectIds.Contains(t.ProjectId))
                    .Select(t => t.Id);

                query = query.Where(a => accessibleTaskIds.Contains(a.TaskItemId));
            }

            query = query.ApplyFiltering(queryParams, AttachmentFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search;
                query = query.Where(a => a.FileName.Contains(search));
            }

            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.Attachments, x => x.Id);

            var projected = query.ProjectTo<AttachmentReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Attachments retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<AttachmentReadDto> CreateAsync(AttachmentCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(dto.TaskItemId, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("CreateAttachment failed. Task not found. TaskId: {TaskId}", dto.TaskItemId);
                throw new NotFoundException("Task not found.");
            }

            // WORKSPACE PIVOT: the FK is now a member id; resolve the caller's membership
            // within the task's project (same workspace scope used by the Authorization pipeline).
            var uploaderMemberId = await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == task.ProjectId && pm.WorkspaceMember.UserId == currentUserId)
                .Select(pm => pm.WorkspaceMemberId)
                .FirstOrDefaultAsync(cancellationToken);
            if (uploaderMemberId == 0)
                throw new ForbiddenException("You are not a member of this task's project.");

            // MEMBERSHIP: dto.TaskItemId is already the value we need - no entity to load yet,
            // so this is straight from the incoming DTO, not a fresh Task query.
            var canAccessTask = await _membershipService.CanAccessTaskAsync(dto.TaskItemId, currentUserId, cancellationToken);
            if (!canAccessTask)
            {
                _logger.LogWarning("CreateAttachment forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, dto.TaskItemId);
                throw new ForbiddenException("You are not a member of this task's project.");
            }

            var attachment = _mapper.Map<Attachment>(dto);
            attachment.UploadedByWorkspaceMemberId = uploaderMemberId;

            // WORKSPACE PIVOT: resolve the parent project for the audit trail (same
            // scope used by the Authorization pipeline).
            var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("CreateAttachment failed. Parent project not found. ProjectId: {ProjectId}", task.ProjectId);
                throw new NotFoundException("Attachment's project not found.");
            }

            await _unitOfWork.Attachments.AddAsync(attachment, cancellationToken);
            var newValues = JsonSerializer.Serialize(new
            {
                attachment.FileName,
                attachment.FilePath,
                attachment.TaskItemId,
                attachment.UploadedByWorkspaceMemberId
            });
            await _unitOfWork.CompleteAsync(cancellationToken);
            await _auditLogService.LogAsync(
                currentUserId,
                "Create Attachment",
                nameof(Attachment),
                attachment.Id.ToString(),
                workspaceId: project.WorkspaceId,
                newValues: newValues,
                cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Attachment uploaded successfully. AttachmentId: {AttachmentId}, TaskId: {TaskId}, UserId: {UserId}",
                attachment.Id, dto.TaskItemId, currentUserId);

            return _mapper.Map<AttachmentReadDto>(attachment);
        }

        // Authorization Pipeline: Visibility → Permission (Attachments.Delete) → Resource Condition
        // (AttachmentUploaderOnlyCondition — uploader-only, all roles, no Owner/Admin bypass per
        // S-14) → Operation (soft delete). BR-AUD-01 excludes attachment operations from the audit log.
        public async Task DeleteAsync(long id, string currentUserId, CancellationToken cancellationToken = default)
        {
            // 1. Load Entity.
            var attachment = await _unitOfWork.Attachments.GetByIdAsync(id, cancellationToken);
            // 2. NotFound.
            if (attachment == null)
            {
                _logger.LogWarning("DeleteAttachment failed. Not found. AttachmentId: {AttachmentId}", id);
                throw new NotFoundException("Attachment not found.");
            }

            // 3. WORKSPACE PIVOT: resolve the workspace through attachment → task → project, the
            // same scope used by the Authorization pipeline.
            var task = await _unitOfWork.Tasks.GetByIdAsync(attachment.TaskItemId, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("DeleteAttachment failed. Parent task not found. TaskId: {TaskId}", attachment.TaskItemId);
                throw new NotFoundException("Attachment's task not found.");
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("DeleteAttachment failed. Parent project not found. ProjectId: {ProjectId}", task.ProjectId);
                throw new NotFoundException("Attachment's project not found.");
            }

            // === Authorization Pipeline (المراحل 1+2+3) ===
            var authResult = await _authService.AuthorizeAsync(
                project.WorkspaceId,
                currentUserId,
                Permissions.AttachmentsDelete,
                new AttachmentUploaderOnlyCondition(attachment));

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "DeleteAttachment pipeline failed. Reason: {Reason}, UserId: {UserId}, AttachmentId: {AttachmentId}",
                    authResult.FailureReason, currentUserId, id);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            // 5. Execute (soft delete).
            // Deleting the physical file from disk/blob storage should happen in the controller/infrastructure
            // layer, since this Service only owns the DB record.
            _unitOfWork.Attachments.Delete(attachment);

            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Attachment deleted successfully. AttachmentId: {AttachmentId}", id);
        }
    }
}
