using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.TaskAssignment;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;

namespace TaskManager.API.Services
{
    // Assign/Unassign live in TaskService (they mutate Task state directly).
    // This service only exposes read/listing for admin & reporting views.
    public class TaskAssignmentService : ITaskAssignmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskAssignmentService> _logger;

        public TaskAssignmentService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<TaskAssignmentService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<TaskAssignmentReadDto>> GetAllAsync(TaskAssignmentQueryParams queryParams, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.TaskAssignments.GetAllQuery().AsNoTracking();

            query = query.ApplyFiltering(queryParams, TaskAssignmentFilterConfig.map);
            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.TaskAssignments, x => x.Id);

            var projected = query.ProjectTo<TaskAssignmentReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Task assignments retrieved successfully. Page: {Page}, PageSize: {PageSize}, Count: {Count}",
                queryParams.Page,
                queryParams.PageSize,
                result.Data.Count);
            return result;
        }
    }
}