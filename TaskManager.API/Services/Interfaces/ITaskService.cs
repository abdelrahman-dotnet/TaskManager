using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Task;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ITaskService
    {
        Task<PagedResult<TaskReadDto>> GetAllAsync(
            TaskQueryParam queryParams,
            string currentUserId,
            CancellationToken cancellationToken = default);

        Task<TaskDetailsReadDto> GetByIdAsync(
            long id,
            string currentUserId,
            CancellationToken cancellationToken = default);

        Task<TaskReadDto> CreateAsync(
            TaskCreateDto dto,
            string currentUserId,
            CancellationToken cancellationToken = default);
        Task<TaskReadDto> UpdateAsync(
            long id,
            TaskUpdateDto dto,
            string currentUserId,
            CancellationToken cancellationToken = default);

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Tasks.Delete) -> Resource Condition -> Operation.
        Task DeleteAsync(
            long id,
            long workspaceId,
            string currentUserId,
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
            ChangeTaskItemStatusDto dto,
            string currentUserId,
            CancellationToken cancellationToken = default);

        Task<TaskReadDto> ChangePriorityAsync(
            long taskId,
            ChangeTaskPriorityDto dto,
            string currentUserId,
            CancellationToken cancellationToken = default);

        // PIPELINE: Visibility -> Permission (TasksArchive) -> Condition (TaskArchivedCondition) -> Operation (BR-TSK-05).
        Task ArchiveAsync(
            long taskId,
            string currentUserId,
            CancellationToken cancellationToken = default);

        // PIPELINE: Visibility -> Permission (TasksRestore) -> Operation (BR-TSK-05).
        Task RestoreAsync(
            long taskId,
            string currentUserId,
            CancellationToken cancellationToken = default);
    }
}