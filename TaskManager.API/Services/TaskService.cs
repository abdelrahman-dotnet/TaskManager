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

        public TaskService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<TaskService> logger, IAuditLogService auditLogService, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _auditLogService = auditLogService;
            _userManager = userManager;
        }

        public async Task<PagedResult<TaskReadDto>> GetAllAsync(TaskQueryParam queryParams, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Tasks.GetAllQuery().AsNoTracking();

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

            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize,cancellationToken);

            _logger.LogInformation("Tasks retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<TaskDetailsReadDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetDetailsAsync(id,cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("GetTaskById failed. Task not found. TaskId: {TaskId}", id);
                throw new NotFoundException("Task not found.");
            }

            return _mapper.Map<TaskDetailsReadDto>(task);
        }

        public async Task<TaskReadDto> CreateAsync(TaskCreateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            if (dto.DueDate.HasValue && dto.DueDate.Value.Date < DateTime.UtcNow.Date)
                throw new BadRequestException("Due date cannot be in the past.");

            var projectExists = await _unitOfWork.Projects.ExistsAsync(p => p.Id == dto.ProjectId,cancellationToken);
            if (!projectExists)
                throw new NotFoundException("Project not found.");

            var task = _mapper.Map<TaskItem>(dto);
            task.Status = TaskItemStatus.Todo;
            task.CreatedByUserId = currentUserId;
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Tasks.AddAsync(task,cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(currentUserId,"Create Task",nameof(TaskItem),task.Id.ToString());
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Task created successfully. TaskId: {TaskId}, UserId: {UserId}", task.Id, currentUserId);
            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> UpdateAsync(long id, TaskUpdateDto dto, string currentUserId,bool canManageAny, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id,cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("UpdateTask failed. Task not found. TaskId: {TaskId}", id);
                throw new NotFoundException("Task not found.");
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
            await _auditLogService.LogAsync(currentUserId,"Update Task",nameof(TaskItem),task.Id.ToString());
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task updated successfully. TaskId: {TaskId}, UserId: {UserId}", id, currentUserId);
            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task DeleteAsync(long id, string currentUserId,bool canManageAny, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id,cancellationToken);
            if (task == null)
            {
                _logger.LogWarning("DeleteTask failed. Task not found. TaskId: {TaskId}", id);
                throw new NotFoundException("Task not found.");
            }

            if (!canManageAny && task.CreatedByUserId != currentUserId)
            {
                _logger.LogWarning("DeleteTask forbidden. UserId: {UserId} tried to delete TaskId: {TaskId}", currentUserId, id);
                throw new ForbiddenException("You can only delete tasks you created.");
            }

            _unitOfWork.Tasks.Delete(task);
            await _auditLogService.LogAsync(currentUserId,"Delete Task",nameof(TaskItem),id.ToString());
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task deleted successfully. TaskId: {TaskId}, UserId: {UserId}", id, currentUserId);
        }

        public async Task<TaskReadDto> AssignAsync(long taskId, AssignTaskDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId,cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");
            if (task.Status == TaskItemStatus.Done)
                throw new BadRequestException("you cannot Assign Completed tasks.");
            var alreadyAssigned = await _unitOfWork.TaskAssignments.ExistsAsync(
                a => a.TaskItemId == taskId && a.UserId == dto.UserId, cancellationToken);

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                _logger.LogWarning("Assign Failed.User not found. UserId: {UserId}", dto.UserId);
                throw new NotFoundException("User not found.");
            }
            if (alreadyAssigned)
                throw new ConflictException("User is already assigned to this task.");
            
            var assignment = new TaskAssignment
            {
                TaskItemId = taskId,
                UserId = dto.UserId,
                AssignedByUserId = currentUserId,
                AssignedAt = DateTime.UtcNow
            };

            await _unitOfWork.TaskAssignments.AddAsync(assignment,cancellationToken);
            await _auditLogService.LogAsync(currentUserId, "Assign Task",nameof(TaskAssignment),taskId.ToString());
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task assigned successfully. TaskId: {TaskId}, UserId: {UserId}, AssignedBy: {AssignedByUserId}",
                taskId, dto.UserId, currentUserId);

            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> UnassignAsync(long taskId,string currentUserId,string userId,CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");
            var assignment = await _unitOfWork.TaskAssignments.FirstOrDefaultAsync(a => a.TaskItemId == taskId && a.UserId == userId,cancellationToken);
            if (assignment == null)
                throw new NotFoundException("Assignment not found.");
            _unitOfWork.TaskAssignments.Delete(assignment);

            await _auditLogService.LogAsync(currentUserId,"Unassign Task",nameof(TaskItem),taskId.ToString(),oldValues: $"AssignedUser:{userId}",newValues: null);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task unassigned successfully. TaskId: {TaskId}, UserId: {UserId}, PerformedBy: {CurrentUserId}",taskId,userId,currentUserId);

            return _mapper.Map<TaskReadDto>(task);
        }

        public async Task<TaskReadDto> ChangeStatusAsync(long taskId, ChangeTaskStatusDto dto, string currentUserId,CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId,cancellationToken);
            if (task == null)
                throw new NotFoundException("Task not found.");

            if (task.Status == TaskItemStatus.Done && dto.NewStatus != TaskItemStatus.Done)
                throw new BadRequestException("Completed tasks cannot move back to a previous status.");
            if(task.Status==dto.NewStatus)
                throw new BadRequestException("Task is already in the requested status."); 
            var oldStatus = task.Status;

            task.Status = dto.NewStatus;
            task.UpdatedAt = DateTime.UtcNow;
            if (dto.NewStatus == TaskItemStatus.Done)
                task.CompletedAt = DateTime.UtcNow;

            _unitOfWork.Tasks.Update(task);

            var history = new TaskStatusHistory
            {
                TaskItemId = taskId,
                OldStatus = oldStatus,
                NewStatus = dto.NewStatus,
                ChangedByUserId = currentUserId,
                ChangedAt = DateTime.UtcNow
            };
            await _unitOfWork.TaskStatusHistories.AddAsync(history,cancellationToken);
            await _auditLogService.LogAsync(currentUserId,"Change Task Status",nameof(TaskItem),taskId.ToString());
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Task status changed. TaskId: {TaskId}, {OldStatus} -> {NewStatus}, By: {UserId}",
                taskId, oldStatus, dto.NewStatus, currentUserId);

            return _mapper.Map<TaskReadDto>(task);
        }
        public async Task<TaskReadDto> ChangePriorityAsync(long taskId,ChangeTaskPriorityDto dto,string currentUserId,CancellationToken cancellationToken = default)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId, cancellationToken);

            if (task is null)
            {
                _logger.LogWarning("ChangePriority failed. Task not found. TaskId: {TaskId}", taskId);
                throw new NotFoundException("Task not found.");
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