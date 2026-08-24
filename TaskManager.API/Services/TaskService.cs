using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Notification;
using TaskManager.API.DTOs.Task;
using TaskManager.API.Exceptions;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Bussiness.Authorization;
using TaskManager.Bussiness.Authorization.ResourceConditions;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Data.Enums;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWorkspaceAuthorizationService _authService;
        // D-29 trigger: Task.Assign -> TaskAssigned notification for the assignee.
        private readonly INotificationService _notificationService;

        public TaskService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<TaskService> logger,
            IAuditLogService auditLogService,
            UserManager<ApplicationUser> userManager,
            IWorkspaceAuthorizationService authService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _auditLogService = auditLogService;
            _userManager = userManager;
            _authService = authService;
            _notificationService = notificationService;
        }

        // RESOLVE WORKSPACE: Task -> Project -> Workspace. Used by the Authorization
        // Pipeline (Visibility stage) on every task operation.
        private async Task<(TaskItem task, long workspaceId)> ResolveTaskWorkspaceAsync(
            long id,
            CancellationToken cancellationToken)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("Task not found. TaskId: {TaskId}", id);
                throw new NotFoundException("Task not found.");
            }

            var workspaceId = await _unitOfWork.Projects.GetAllQuery()
                .Where(p => p.Id == task.ProjectId)
                .Select(p => (long?)p.WorkspaceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (workspaceId == null)
            {
                _logger.LogWarning("Task's project has no workspace. TaskId: {TaskId}, ProjectId: {ProjectId}", id, task.ProjectId);
                throw new NotFoundException("Task's project not found.");
            }

            return (task, workspaceId.Value);
        }

        // RESOLVE PROJECT WORKSPACE: Project -> Workspace. Used for Create and read checks.
        private async Task<long> ResolveProjectWorkspaceAsync(long projectId, CancellationToken cancellationToken)
        {
            var workspaceId = await _unitOfWork.Projects.GetAllQuery()
                .Where(p => p.Id == projectId)
                .Select(p => (long?)p.WorkspaceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (workspaceId == null)
            {
                _logger.LogWarning("Project not found or has no workspace. ProjectId: {ProjectId}", projectId);
                throw new NotFoundException("Project not found.");
            }

            return workspaceId.Value;
        }

        // VISIBILITY (read-listing): filters to tasks in projects the user belongs to, done
        // as part of the query itself (an IN subquery against ProjectMembers). There is no
        // single workspaceId for a cross-workspace list, so read-listing keeps the
        // membership-scoped query rather than the per-workspace pipeline.
        public async Task<PagedResult<TaskReadDto>> GetAllAsync(TaskQueryParam queryParams, string currentUserId, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Tasks.GetAllQuery().AsNoTracking();

            // G-5: Trash = soft-deleted tasks (IsDeleted), not archived. The global query
            // filter hides soft-deleted rows, so include them explicitly only when the
            // deleted filter is requested (ViewTrash / deleted-list consumers).
            if (queryParams.IsDeleted.HasValue)
            {
                query = query.IgnoreQueryFilters().Where(t => t.IsDeleted == queryParams.IsDeleted.Value);
            }

            // VISIBILITY: tasks in projects the user is directly a member of OR projects
            // attached to teams the user belongs to (Project ↔ Team M:N — D-06/D-20).
            var memberProjectIds = _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.WorkspaceMember.UserId == currentUserId)
                .Select(pm => pm.ProjectId);

            var teamProjectIds = _unitOfWork.ProjectTeams.GetAllQuery()
                .Where(pt => pt.Team.TeamMembers.Any(tm => tm.WorkspaceMember.UserId == currentUserId))
                .Select(pt => pt.ProjectId);

            query = query.Where(t => memberProjectIds.Contains(t.ProjectId)
                || teamProjectIds.Contains(t.ProjectId));

            query = query.ApplyFiltering(queryParams, TaskFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search;
                query = query.Where(t =>
                    t.Title.Contains(search) ||
                    t.Description != null && t.Description.Contains(search));
            }

            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.Tasks, x => x.Id);

            var projected = query.ProjectTo<TaskReadDto>(_mapper.ConfigurationProvider);

            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Tasks retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<PagedResult<TaskReadDto>> GetTrashAsync(
            long workspaceId,
            string currentUserId,
            CancellationToken cancellationToken = default)
        {
            // Trash is workspace-scoped so it can use the established pipeline:
            // Visibility -> Tasks.ViewTrash permission (Owner/Admin) -> operation.
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TasksViewTrash);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "ViewTrash pipeline failed. Reason: {Reason}, UserId: {UserId}, WorkspaceId: {WorkspaceId}",
                    authResult.FailureReason, currentUserId, workspaceId);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message ?? "Resource not found.")
                    : new ForbiddenException(authResult.Message ?? "You are not authorized to perform this action.");
            }

            // Trash semantics are intentionally IsDeleted=true. Ignore the global
            // soft-delete filter only for this authorized trash query.
            var workspaceProjectIds = _unitOfWork.Projects.GetAllQuery()
                .Where(project => project.WorkspaceId == workspaceId)
                .Select(project => project.Id);

            var query = _unitOfWork.Tasks.GetAllQuery()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(task => task.IsDeleted && workspaceProjectIds.Contains(task.ProjectId));

            var projected = query.ProjectTo<TaskReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(1, 10, cancellationToken);

            _logger.LogInformation(
                "Workspace trash retrieved successfully. Count: {Count}, WorkspaceId: {WorkspaceId}",
                result.Data.Count, workspaceId);

            return result;
        }

        public async Task<TaskDetailsReadDto> GetByIdAsync(long id, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetDetailsAsync(id, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("GetTaskById failed. Task not found. TaskId: {TaskId}", id);
                throw new NotFoundException("Task not found.");
            }

            // TASK PIVOT: Task -> Project -> Workspace for pipeline stage 1 (Visibility).
            var workspaceId = await ResolveProjectWorkspaceAsync(task.ProjectId, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.WorkspaceView);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "GetTaskById pipeline failed. Reason: {Reason}, UserId: {UserId}, TaskId: {TaskId}",
                    authResult.FailureReason, currentUserId, id);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            return _mapper.Map<TaskDetailsReadDto>(task);
        }

        public async Task<TaskReadDto> CreateAsync(TaskCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            if (dto.DueDate.HasValue && dto.DueDate.Value.Date < DateTime.UtcNow.Date)
                throw new BadRequestException("Due date cannot be in the past.");

            // TASK PIVOT: project lookup + workspace resolution (NotFound if missing), then
            // the Authorization Pipeline (Visibility -> Permission: Tasks.Create) -> Operation.
            var workspaceId = await ResolveProjectWorkspaceAsync(dto.ProjectId, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TasksCreate);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "CreateTask pipeline failed. Reason: {Reason}, UserId: {UserId}, ProjectId: {ProjectId}",
                    authResult.FailureReason, currentUserId, dto.ProjectId);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            // WORKSPACE PIVOT: the task creator FK is a WorkspaceMember. Resolve the
            // already-authorized caller inside the task's resolved workspace; this is
            // required relational data, not a second authorization mechanism.
            var creatorMember = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.WorkspaceId == workspaceId
                             && wm.UserId == currentUserId
                             && wm.Status == WorkspaceMemberStatus.Active)
                .FirstOrDefaultAsync(cancellationToken);
            if (creatorMember is null)
            {
                _logger.LogWarning("CreateTask failed. Active workspace membership not found. UserId: {UserId}, WorkspaceId: {WorkspaceId}", currentUserId, workspaceId);
                throw new NotFoundException("You are not an active member of this workspace.");
            }

            var task = _mapper.Map<TaskItem>(dto);
            task.Status = TaskItemStatus.Todo;
            task.CreatedByUserId = currentUserId;
            task.CreatedByWorkspaceMemberId = creatorMember.Id;
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Tasks.AddAsync(task, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(currentUserId, "Create Task", nameof(TaskItem), task.Id.ToString(), workspaceId);
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Task created successfully. TaskId: {TaskId}, UserId: {UserId}", task.Id, currentUserId);
            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> UpdateAsync(long id, TaskUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            // TASK PIVOT: Task -> Project -> Workspace, then the full pipeline replaces the
            // legacy membership + creator checks (WorkspaceMember.Role is the sole authority
            // per the Master Spec - no creator-only or ManageAny semantics).
            var (task, workspaceId) = await ResolveTaskWorkspaceAsync(id, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TasksUpdate,
                new TaskArchivedCondition(task));

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "UpdateTask pipeline failed. Reason: {Reason}, UserId: {UserId}, TaskId: {TaskId}",
                    authResult.FailureReason, currentUserId, id);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            if (dto.DueDate.HasValue && dto.DueDate.Value.Date < DateTime.UtcNow.Date)
                throw new BadRequestException("Due date cannot be in the past.");

            _mapper.Map(dto, task);
            task.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tasks.Update(task);
            await _auditLogService.LogAsync(currentUserId, "Update Task", nameof(TaskItem), task.Id.ToString(), workspaceId);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task updated successfully. TaskId: {TaskId}, UserId: {UserId}", id, currentUserId);
            return _mapper.Map<TaskReadDto>(task);
        }

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Tasks.Delete) -> Resource
        // Condition (TaskDeleteCondition) -> Operation (Soft Delete).
        public async Task DeleteAsync(long id, long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            // The route scope is a consistency check only. Authorization must always use
            // the authoritative Task -> Project -> Workspace scope.
            var (task, taskWorkspaceId) = await ResolveTaskWorkspaceAsync(id, cancellationToken);

            // === Authorization Pipeline (Ø§Ù„Ù…Ø±Ø§Ø­Ù„ 1+2+3) ===
            var authResult = await _authService.AuthorizeAsync(
                taskWorkspaceId,
                currentUserId,
                Permissions.TasksDelete,
                new TaskDeleteCondition(task));

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "DeleteTask pipeline failed. Reason: {Reason}, UserId: {UserId}, TaskId: {TaskId}",
                    authResult.FailureReason, currentUserId, id);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            // A mismatched route tenant is never an authorization authority. Hide the
            // resource consistently after the real-workspace pipeline has evaluated it.
            if (workspaceId != taskWorkspaceId)
            {
                _logger.LogWarning(
                    "DeleteTask workspace mismatch. RouteWorkspaceId: {RouteWorkspaceId}, TaskWorkspaceId: {TaskWorkspaceId}, UserId: {UserId}, TaskId: {TaskId}",
                    workspaceId, taskWorkspaceId, currentUserId, id);
                throw new NotFoundException("Task not found.");
            }

            // === Ø§Ù„Ù…Ø±Ø­Ù„Ø© 4: Business Rules (Dependencies - future) ===

            // === Ø§Ù„Ù…Ø±Ø­Ù„Ø© 5: Operation (Soft Delete) ===
            if (task.IsArchived)
                throw new BadRequestException("This task is archived. Restore it first before deleting.");
            _unitOfWork.Tasks.Delete(task);
            await _auditLogService.LogAsync(currentUserId, "Delete Task", nameof(TaskItem), id.ToString(), taskWorkspaceId);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task deleted successfully. TaskId: {TaskId}, UserId: {UserId}", id, currentUserId);
        }

        public async Task<TaskReadDto> AssignAsync(long taskId, AssignTaskDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");

            // TASK PIVOT: Task -> Project -> Workspace, then pipeline replaces the legacy
            // membership checks. The assignee still needs a ProjectMember record in the same
            // project (data lookup for the FK) but no separate membership check - the
            // permission stage covers workspace membership.
            var workspaceId = await ResolveProjectWorkspaceAsync(task.ProjectId, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TasksAssign,
                new TaskArchivedCondition(task));

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "AssignTask pipeline failed. Reason: {Reason}, UserId: {UserId}, TaskId: {TaskId}",
                    authResult.FailureReason, currentUserId, taskId);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            // === Ø§Ù„Ù…Ø±Ø­Ù„Ø© 4: Business Rules ===
            if (task.Status == TaskItemStatus.Done)
                throw new BadRequestException("you cannot Assign Completed tasks.");
            var alreadyAssigned = await _unitOfWork.TaskAssignments.ExistsAsync(
                a => a.TaskItemId == taskId && a.WorkspaceMember.UserId == dto.UserId, cancellationToken);

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                _logger.LogWarning("Assign Failed.User not found. UserId: {UserId}", dto.UserId);
                throw new NotFoundException("User not found.");
            }
            if (alreadyAssigned)
                throw new ConflictException("User is already assigned to this task.");

            // WORKSPACE PIVOT: TaskAssignment now points at WorkspaceMember (long FK).
            // Resolve both members within the task's project.
            var assigneeMemberId = await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == task.ProjectId && pm.WorkspaceMember.UserId == dto.UserId)
                .Select(pm => pm.WorkspaceMemberId)
                .FirstOrDefaultAsync(cancellationToken);
            if (assigneeMemberId == 0)
                throw new BadRequestException("The user being assigned must be a member of this task's project.");
            var assignerMemberId = await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == task.ProjectId && pm.WorkspaceMember.UserId == currentUserId)
                .Select(pm => pm.WorkspaceMemberId)
                .FirstOrDefaultAsync(cancellationToken);
            if (assignerMemberId == 0)
                throw new BadRequestException("You are not a member of this task's project.");

            // MEMBERSHIP: the person being assigned should also be a member of the project -
            // assigning work to someone who can't even see the project would be a dead end.
            // Covered by the WorkspaceMemberId lookup above (assigneeMemberId == 0 check),
            // kept explicit for clarity.
            var assigneeCanAccess = await _unitOfWork.ProjectMembers.GetAllQuery()
                .AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.WorkspaceMember.UserId == dto.UserId, cancellationToken);
            if (!assigneeCanAccess)
            {
                _logger.LogWarning("AssignTask failed. Assignee UserId: {AssigneeId} is not a member of TaskId: {TaskId}'s project", dto.UserId, taskId);
                throw new BadRequestException("The user being assigned must be a member of this task's project.");
            }

            var assignment = new TaskAssignment
            {
                TaskItemId = taskId,
                WorkspaceMemberId = assigneeMemberId,
                AssignedByWorkspaceMemberId = assignerMemberId,
                AssignedAt = DateTime.UtcNow
            };

            await _unitOfWork.TaskAssignments.AddAsync(assignment, cancellationToken);
            await _auditLogService.LogAsync(currentUserId, "Assign Task", nameof(TaskAssignment), taskId.ToString(), workspaceId);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task assigned successfully. TaskId: {TaskId}, UserId: {UserId}, AssignedBy: {AssignedByUserId}",
                taskId, dto.UserId, currentUserId);

            // D-29 NOTIFICATION TRIGGER: Task.Assign -> TaskAssigned for the assignee.
            try
            {
                await _notificationService.CreateAsync(
                    new NotificationCreateDto
                    {
                        WorkspaceId = workspaceId,
                        UserId = dto.UserId,
                        Title = "Task Assigned",
                        Message = $"You have been assigned to task \"{task.Title}\"."
                    },
                    currentUserId,
                    triggeringPermission: Permissions.TasksAssign);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Task.Assign notification failed (non-blocking). TaskId: {TaskId}, AssigneeId: {AssigneeId}", taskId, dto.UserId);
            }

            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> UnassignAsync(long taskId, string currentUserId, string userId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");

            // TASK PIVOT: Task -> Project -> Workspace, then pipeline replaces the legacy
            // membership check (same permission as Assign - Tasks.Assign covers both).
            var workspaceId = await ResolveProjectWorkspaceAsync(task.ProjectId, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TasksAssign);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "UnassignTask pipeline failed. Reason: {Reason}, UserId: {UserId}, TaskId: {TaskId}",
                    authResult.FailureReason, currentUserId, taskId);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            var assignment = await _unitOfWork.TaskAssignments.FirstOrDefaultAsync(a => a.TaskItemId == taskId && a.WorkspaceMember.UserId == userId, cancellationToken);
            if (assignment == null)
                throw new NotFoundException("Assignment not found.");
            _unitOfWork.TaskAssignments.Delete(assignment);

            await _auditLogService.LogAsync(currentUserId, "Unassign Task", nameof(TaskItem), taskId.ToString(), workspaceId: workspaceId, oldValues: $"AssignedUser:{userId}", newValues: null, cancellationToken: cancellationToken);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task unassigned successfully. TaskId: {TaskId}, UserId: {UserId}, PerformedBy: {CurrentUserId}", taskId, userId, currentUserId);

            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> ChangeStatusAsync(long taskId, ChangeTaskItemStatusDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");

            // TASK PIVOT: Task -> Project -> Workspace, then pipeline replaces the legacy
            // membership check. The actor's member id (for the history FK) is still resolved
            // from the ProjectMember pivot - this is a data lookup, not an authorization.
            var workspaceId = await ResolveProjectWorkspaceAsync(task.ProjectId, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TasksChangeStatus,
                new TaskArchivedCondition(task));

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "ChangeStatus pipeline failed. Reason: {Reason}, UserId: {UserId}, TaskId: {TaskId}",
                    authResult.FailureReason, currentUserId, taskId);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            var changerMemberId = await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == task.ProjectId && pm.WorkspaceMember.UserId == currentUserId)
                .Select(pm => pm.WorkspaceMemberId)
                .FirstOrDefaultAsync(cancellationToken);
            if (changerMemberId == 0)
                throw new BadRequestException("You are not a member of this task's project.");

            // === Ø§Ù„Ù…Ø±Ø­Ù„Ø© 4: Business Rules (BR-TSK-03 state machine) ===
            if (task.Status == TaskItemStatus.Done && dto.NewStatus != TaskItemStatus.Done)
                throw new BadRequestException("Completed tasks cannot move back to a previous status.");
            if (task.Status == dto.NewStatus)
                throw new BadRequestException("Task is already in the requested status.");
            var oldStatus = task.Status;

            task.Status = dto.NewStatus;
            task.UpdatedAt = DateTime.UtcNow;
            if (dto.NewStatus == TaskItemStatus.Done)
                task.CompletedAt = DateTime.UtcNow;

            _unitOfWork.Tasks.Update(task);

            var history = new TaskItemStatusHistory
            {
                TaskItemId = taskId,
                OldStatus = oldStatus,
                NewStatus = dto.NewStatus,
                ChangedByWorkspaceMemberId = changerMemberId,
                ChangedAt = DateTime.UtcNow
            };
            await _unitOfWork.TaskItemStatusHistories.AddAsync(history, cancellationToken);
            await _auditLogService.LogAsync(currentUserId, "Change Task Status", nameof(TaskItem), taskId.ToString(), workspaceId);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task status changed. TaskId: {TaskId}, {OldStatus} -> {NewStatus}, By: {UserId}",
                taskId, oldStatus, dto.NewStatus, currentUserId);

            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> ChangePriorityAsync(long taskId, ChangeTaskPriorityDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            // TASK PIVOT: Task -> Project -> Workspace, then pipeline replaces the legacy
            // membership check.
            var (task, workspaceId) = await ResolveTaskWorkspaceAsync(taskId, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TasksChangePriority,
                new TaskArchivedCondition(task));

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "ChangePriority pipeline failed. Reason: {Reason}, UserId: {UserId}, TaskId: {TaskId}",
                    authResult.FailureReason, currentUserId, taskId);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            if (task.Priority == dto.NewPriority)
            {
                _logger.LogWarning(
                    "ChangePriority failed. Task already has the same priority. TaskId: {TaskId}",
                    taskId);

                throw new BadRequestException("Task already has the requested priority.");
            }

            task.Priority = dto.NewPriority;
            task.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tasks.Update(task);

            await _auditLogService.LogAsync(
                currentUserId,
                "Change Priority",
                nameof(TaskItem),
                task.Id.ToString(),
                oldValues: task.Priority.ToString(),
                workspaceId: workspaceId,
                newValues: dto.NewPriority.ToString());

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation(
                "Task priority changed successfully. TaskId: {TaskId}, NewPriority: {Priority}, UserId: {UserId}",
                task.Id,
                dto.NewPriority,
                currentUserId);

            return _mapper.Map<TaskReadDto>(task);
        }

        // PIPELINE: Visibility -> Permission (Tasks.Archive) -> Resource Condition
        // (TaskArchivedCondition - cannot archive twice) -> Operation (BR-TSK-05 soft archive).
        public async Task ArchiveAsync(long taskId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");

            var workspaceId = await ResolveProjectWorkspaceAsync(task.ProjectId, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TasksArchive,
                new TaskArchivedCondition(task));

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("ArchiveTask pipeline failed. Reason: {Reason}, UserId: {UserId}, TaskId: {TaskId}",
                    authResult.FailureReason, currentUserId, taskId);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            if (task.IsArchived)
                throw new BadRequestException("Task is already archived.");

            task.IsArchived = true;
            task.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Tasks.Update(task);
            await _auditLogService.LogAsync(currentUserId, "Archive Task", nameof(TaskItem), taskId.ToString(), workspaceId);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task archived. TaskId: {TaskId}, By: {UserId}", taskId, currentUserId);
        }

        // PIPELINE: Visibility -> Permission (Tasks.Restore) -> Operation (BR-TSK-05).
        public async Task RestoreAsync(long taskId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");

            var workspaceId = await ResolveProjectWorkspaceAsync(task.ProjectId, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TasksRestore);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("RestoreTask pipeline failed. Reason: {Reason}, UserId: {UserId}, TaskId: {TaskId}",
                    authResult.FailureReason, currentUserId, taskId);

                throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                    ? new NotFoundException(authResult.Message)
                    : new ForbiddenException(authResult.Message);
            }

            if (!task.IsArchived)
                throw new BadRequestException("Task is not archived.");

            task.IsArchived = false;
            task.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Tasks.Update(task);
            await _auditLogService.LogAsync(currentUserId, "Restore Task", nameof(TaskItem), taskId.ToString(), workspaceId);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task restored. TaskId: {TaskId}, By: {UserId}", taskId, currentUserId);
        }
    }
}