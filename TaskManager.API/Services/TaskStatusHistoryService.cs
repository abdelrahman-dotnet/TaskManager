using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.TaskStatusHistory;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;

namespace TaskManager.API.Services
{
    // Read-only. Entries are created internally by TaskService.ChangeStatusAsync.
    public class TaskStatusHistoryService : ITaskStatusHistoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskStatusHistoryService> _logger;

        public TaskStatusHistoryService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<TaskStatusHistoryService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<TaskStatusHistoryReadDto>> GetAllAsync(TaskStatusHistoryQueryParams queryParams, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.TaskStatusHistories.GetAllQuery().AsNoTracking();

            query = query.ApplyFiltering(queryParams, TaskStatusHistoryFilterConfig.map);
            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.TaskStatusHistories, x => x.Id);

            var projected = query.ProjectTo<TaskStatusHistoryReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Task status histories retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }
    }
}