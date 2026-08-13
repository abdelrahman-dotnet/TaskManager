using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Task;
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
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMembershipService _membershipService;
        private readonly IWorkspaceAuthorizationService _authService;

        public TaskService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<TaskService> logger,
            IAuditLogService auditLogService,
            UserManager<ApplicationUser> userManager,
            IMembershipService membershipService,
            IWorkspaceAuthorizationService authService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _auditLogService = auditLogService;
            _userManager = userManager;
            _membershipService = membershipService;
            _authService = authService;
        }

        // MEMBERSHIP: filters to tasks in projects the user belongs to, done as part of the
        // query itself (an IN subquery against ProjectMembers), not fetched-then-filtered.
        // canManageAny (Tasks.ManageAny) bypasses the filter entirely.
        public async Task<PagedResult<TaskReadDto>> GetAllAsync(TaskQueryParam queryParams, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Tasks.GetAllQuery().AsNoTracking();

            if (!canManageAny)
            {
                var memberProjectIds = _unitOfWork.ProjectMembers.GetAllQuery()
                    .Where(pm => pm.WorkspaceMember.UserId == currentUserId)
                    .Select(pm => pm.ProjectId);

                query = query.Where(t => memberProjectIds.Contains(t.ProjectId));
            }

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

        // MEMBERSHIP: NotFound if the task truly doesn't exist; Forbidden if it exists but the
        // caller isn't a member of its project (and doesn't have canManageAny). Kept as two
        // distinct exceptions rather than always NotFound, consistent with how Forbidden is
        // used everywhere else in this codebase (e.g. TaskService.UpdateAsync above).
        public async Task<TaskDetailsReadDto> GetByIdAsync(long id, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetDetailsAsync(id, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("GetTaskById failed. Task not found. TaskId: {TaskId}", id);
                throw new NotFoundException("Task not found.");
            }

            if (!canManageAny)
            {
                var canAccess = await _membershipService.CanAccessTaskAsync(id, currentUserId, cancellationToken);
                if (!canAccess)
                {
                    _logger.LogWarning("GetTaskById forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, id);
                    throw new ForbiddenException("You are not a member of this task's project.");
                }
            }

            return _mapper.Map<TaskDetailsReadDto>(task);
        }

        public async Task<TaskReadDto> CreateAsync(TaskCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            if (dto.DueDate.HasValue && dto.DueDate.Value.Date < DateTime.UtcNow.Date)
                throw new BadRequestException("Due date cannot be in the past.");

            var projectExists = await _unitOfWork.Projects.ExistsAsync(p => p.Id == dto.ProjectId, cancellationToken);
            if (!projectExists)
                throw new NotFoundException("Project not found.");

            // MEMBERSHIP: must belong to the target Project to create tasks in it.
            var canAccessProject = await _membershipService.CanAccessProjectAsync(dto.ProjectId, currentUserId, cancellationToken);
            if (!canAccessProject)
            {
                _logger.LogWarning("CreateTask forbidden. UserId: {UserId} is not a member of ProjectId: {ProjectId}", currentUserId, dto.ProjectId);
                throw new ForbiddenException("You must be a member of the project to create tasks in it.");
            }

            var task = _mapper.Map<TaskItem>(dto);
            task.Status = TaskItemStatus.Todo;
            task.CreatedByUserId = currentUserId;
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Tasks.AddAsync(task, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(currentUserId, "Create Task", nameof(TaskItem), task.Id.ToString());
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Task created successfully. TaskId: {TaskId}, UserId: {UserId}", task.Id, currentUserId);
            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> UpdateAsync(long id, TaskUpdateDto dto, string currentUserId, bool canManageAny, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("UpdateTask failed. Task not found. TaskId: {TaskId}", id);
                throw new NotFoundException("Task not found.");
            }

            // MEMBERSHIP: must belong to the task's Project, regardless of ownership/ManageAny -
            // ManageAny bypasses the "did I create this task" check below, but not Membership.
            var canAccessTask = await _membershipService.CanAccessTaskAsync(id, currentUserId, cancellationToken);
            if (!canAccessTask)
            {
                _logger.LogWarning("UpdateTask forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, id);
                throw new ForbiddenException("You are not a member of this task's project.");
            }

            if (!canManageAny && task.CreatedByUserId != currentUserId)
            {
                _logger.LogWarning("UpdateTask forbidden. UserId: {UserId} tried to edit TaskId: {TaskId}", currentUserId, id);
                throw new ForbiddenException("You can only edit tasks you created.");
            }

            if (dto.DueDate.HasValue && dto.DueDate.Value.Date < DateTime.UtcNow.Date)
                throw new BadRequestException("Due date cannot be in the past.");

            _mapper.Map(dto, task);
            task.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tasks.Update(task);
            await _auditLogService.LogAsync(currentUserId, "Update Task", nameof(TaskItem), task.Id.ToString());
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task updated successfully. TaskId: {TaskId}, UserId: {UserId}", id, currentUserId);
            return _mapper.Map<TaskReadDto>(task);
        }

        // PIPELINE (Auth Pipeline — Pilot on Tasks.Delete):
        // Visibility → Permission → Resource Condition (TaskDeleteCondition) → Operation.
        public async Task DeleteAsync(long id, long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id, cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("DeleteTask failed. Task not found. TaskId: {TaskId}", id);
                throw new NotFoundException("Task not found.");
            }

            // === Authorization Pipeline (المراحل 1+2+3) ===
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
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

            // === المرحلة 4: Business Rules (لو اتضافت لاحقًا — Dependencies مثلاً) ===

            // === المرحلة 5: Operation (Soft Delete) ===
            _unitOfWork.Tasks.Delete(task);
            await _auditLogService.LogAsync(currentUserId, "Delete Task", nameof(TaskItem), id.ToString());
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task deleted successfully. TaskId: {TaskId}, UserId: {UserId}", id, currentUserId);
        }

        public async Task<TaskReadDto> AssignAsync(long taskId, AssignTaskDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");

            // MEMBERSHIP: the person doing the assigning must belong to the task's project.
            var canAccessTask = await _membershipService.CanAccessTaskAsync(taskId, currentUserId, cancellationToken);
            if (!canAccessTask)
            {
                _logger.LogWarning("AssignTask forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, taskId);
                throw new ForbiddenException("You are not a member of this task's project.");
            }

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
                throw new ForbiddenException("You are not a member of this task's project.");

            // MEMBERSHIP: the person being assigned should also be a member of the project -
            // assigning work to someone who can't even see the project would be a dead end.
            var assigneeCanAccess = await _membershipService.CanAccessTaskAsync(taskId, dto.UserId, cancellationToken);
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
            await _auditLogService.LogAsync(currentUserId, "Assign Task", nameof(TaskAssignment), taskId.ToString());
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task assigned successfully. TaskId: {TaskId}, UserId: {UserId}, AssignedBy: {AssignedByUserId}",
                taskId, dto.UserId, currentUserId);

            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> UnassignAsync(long taskId, string currentUserId, string userId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");

            // MEMBERSHIP: same reasoning as AssignAsync.
            var canAccessTask = await _membershipService.CanAccessTaskAsync(taskId, currentUserId, cancellationToken);
            if (!canAccessTask)
            {
                _logger.LogWarning("UnassignTask forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, taskId);
                throw new ForbiddenException("You are not a member of this task's project.");
            }

            var assignment = await _unitOfWork.TaskAssignments.FirstOrDefaultAsync(a => a.TaskItemId == taskId && a.WorkspaceMember.UserId == userId, cancellationToken);
            if (assignment == null)
                throw new NotFoundException("Assignment not found.");
            _unitOfWork.TaskAssignments.Delete(assignment);

            await _auditLogService.LogAsync(currentUserId, "Unassign Task", nameof(TaskItem), taskId.ToString(), oldValues: $"AssignedUser:{userId}", newValues: null);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task unassigned successfully. TaskId: {TaskId}, UserId: {UserId}, PerformedBy: {CurrentUserId}", taskId, userId, currentUserId);

            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> ChangeStatusAsync(long taskId, ChangeTaskItemStatusDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");

            // WORKSPACE PIVOT: the status-change actor is now a member id, not a user id.
            var changerMemberId = await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == task.ProjectId && pm.WorkspaceMember.UserId == currentUserId)
                .Select(pm => pm.WorkspaceMemberId)
                .FirstOrDefaultAsync(cancellationToken);
            if (changerMemberId == 0)
                throw new ForbiddenException("You are not a member of this task's project.");

            // MEMBERSHIP: must belong to the task's project to change its status.
            var canAccessTask = await _membershipService.CanAccessTaskAsync(taskId, currentUserId, cancellationToken);
            if (!canAccessTask)
            {
                _logger.LogWarning("ChangeStatus forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, taskId);
                throw new ForbiddenException("You are not a member of this task's project.");
            }

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
            await _auditLogService.LogAsync(currentUserId, "Change Task Status", nameof(TaskItem), taskId.ToString());
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task status changed. TaskId: {TaskId}, {OldStatus} -> {NewStatus}, By: {UserId}",
                taskId, oldStatus, dto.NewStatus, currentUserId);

            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> ChangePriorityAsync(long taskId, ChangeTaskPriorityDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);

            if (task is null)
            {
                _logger.LogWarning("ChangePriority failed. Task not found. TaskId: {TaskId}", taskId);
                throw new NotFoundException("Task not found.");
            }

            // MEMBERSHIP: must belong to the task's project to change its priority.
            var canAccessTask = await _membershipService.CanAccessTaskAsync(taskId, currentUserId, cancellationToken);
            if (!canAccessTask)
            {
                _logger.LogWarning("ChangePriority forbidden (Membership). UserId: {UserId}, TaskId: {TaskId}", currentUserId, taskId);
                throw new ForbiddenException("You are not a member of this task's project.");
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
                newValues: dto.NewPriority.ToString());

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation(
                "Task priority changed successfully. TaskId: {TaskId}, NewPriority: {Priority}, UserId: {UserId}",
                task.Id,
                dto.NewPriority,
                currentUserId);

            return _mapper.Map<TaskReadDto>(task);
        }
    }
}
