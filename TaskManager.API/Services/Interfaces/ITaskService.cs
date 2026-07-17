using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Task;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ITaskService
    {
        Task<PagedResult<TaskReadDto>> GetAllAsync(
            TaskQueryParam queryParams,
            CancellationToken cancellationToken = default);

        Task<TaskDetailsReadDto> GetByIdAsync(
            long id,
            CancellationToken cancellationToken = default);

        Task<TaskReadDto> CreateAsync(
            TaskCreateDto dto,
            string currentUserId,
            CancellationToken cancellationToken = default);

        Task<TaskReadDto> UpdateAsync(
            long id,
            TaskUpdateDto dto,
            string currentUserId,
            bool canManageAny,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            long id,
            string currentUserId,
            bool canManageAny,
            CancellationToken cancellationToken = default);

        Task<TaskReadDto> AssignAsync(
            long taskId,
            AssignTaskDto dto,
            string assignedByUserId,
            CancellationToken cancellationToken = default);

        Task<TaskReadDto> UnassignAsync(
            long taskId,
            string userId,
            string currentUserId,
            CancellationToken cancellationToken = default);

        Task<TaskReadDto> ChangeStatusAsync(
            long taskId,
            ChangeTaskStatusDto dto,
            string currentUserId,
            CancellationToken cancellationToken = default);

        Task<TaskReadDto> ChangePriorityAsync(
            long taskId,
            ChangeTaskPriorityDto dto,
            string currentUserId,
            CancellationToken cancellationToken = default);
    }
}