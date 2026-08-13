using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.TaskItemStatusHistory;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;

namespace TaskManager.API.Services
{
    // Read-only. Entries are created internally by TaskService.ChangeStatusAsync.
    public class TaskItemStatusHistoryService : ITaskItemStatusHistoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskItemStatusHistoryService> _logger;

        public TaskItemStatusHistoryService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<TaskItemStatusHistoryService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<TaskItemStatusHistoryReadDto>> GetAllAsync(TaskItemStatusHistoryQueryParams queryParams, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.TaskItemStatusHistories.GetAllQuery().AsNoTracking();

            query = query.ApplyFiltering(queryParams, TaskItemStatusHistoryFilterConfig.map);
            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.TaskItemStatusHistories, x => x.Id);

            var projected = query.ProjectTo<TaskItemStatusHistoryReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Task status histories retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }
    }
}
