using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.AuditLog;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AuditLogService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<AuditLogReadDto>> GetAllAsync(AuditLogQueryParams queryParams, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.AuditLogs.GetAllQuery().AsNoTracking();

            query = query.ApplyFiltering(queryParams, AuditLogFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search;
                query = query.Where(a =>
                     a.Action.Contains(search) ||
                     a.EntityName.Contains(search) ||
                     a.EntityId.Contains(search));
            }

            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.AuditLogs, x => x.Id);

            var projected = query.ProjectTo<AuditLogReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Audit logs retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        // Call this from other services (TaskService, ProjectService, ...) to record an action.
        // Does NOT call CompleteAsync() itself - it participates in the caller's unit-of-work,
        // so the log is only persisted if the surrounding operation succeeds.
        public async Task LogAsync(string? userId, string action, string entityName, string entityId, string? oldValues = null, string? newValues = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(action) ||
                string.IsNullOrWhiteSpace(entityName) ||
                string.IsNullOrWhiteSpace(entityId))
            {
                return;
            }
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues
            };
                
            _logger.LogInformation("Audit log created. Action: {Action}, Entity: {Entity}, EntityId: {EntityId}",
                    action,
                    entityName,
                    entityId);
            await _unitOfWork.AuditLogs.AddAsync(log, cancellationToken);
        }
    }
}