using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.TaskAssignment;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ITaskAssignmentService
    {
        Task<PagedResult<TaskAssignmentReadDto>> GetAllAsync(TaskAssignmentQueryParams queryParams, CancellationToken cancellationToken = default);
    }
}