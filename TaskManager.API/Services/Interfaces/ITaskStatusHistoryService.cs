using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.TaskItemStatusHistory;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ITaskItemStatusHistoryService
    {
        Task<PagedResult<TaskItemStatusHistoryReadDto>> GetAllAsync(TaskItemStatusHistoryQueryParams queryParams, CancellationToken cancellationToken = default);
    }
}
