using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.TaskStatusHistory;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ITaskStatusHistoryService
    {
        Task<PagedResult<TaskStatusHistoryReadDto>> GetAllAsync(TaskStatusHistoryQueryParams queryParams, CancellationToken cancellationToken = default);
    }
}