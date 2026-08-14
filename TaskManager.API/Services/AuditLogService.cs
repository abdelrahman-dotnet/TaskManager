using AutoMapper;
using AutoMapper.QueryableExtensions;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.AuditLog;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Bussiness.Authorization;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AuditLogService> _logger;
        private readonly IWorkspaceAuthorizationService _authService;

        public AuditLogService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AuditLogService> logger, IWorkspaceAuthorizationService authService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _authService = authService;
        }

        // Visibility (stage 1): audit logs are only visible to members of the workspaces
        // the logs belong to. The permission stage (AuditLogsView) is enforced by the
        // controller policy; ResourceCondition is not needed beyond the workspace scope.
        public async Task<PagedResult<AuditLogReadDto>> GetAllAsync(AuditLogQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.AuditLogs.GetAllQuery().AsNoTracking();

            // Scope to workspaces where the caller is an active member (Visibility).
            var memberWorkspaceIds = _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.UserId == currentUserId)
                .Select(wm => wm.WorkspaceId);
            query = query.Where(a => memberWorkspaceIds.Contains(a.WorkspaceId));

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

            var projected = query.Include(a => a.WorkspaceMember).ThenInclude(wm => wm.User)
                .ProjectTo<AuditLogReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Audit logs retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        // Call this from other services (TaskService, ProjectService, ...) to record an action.
        // Does NOT call CompleteAsync() itself - it participates in the caller's unit-of-work,
        // so the log is only persisted if the surrounding operation succeeds.
        public async Task LogAsync(string? userId, string action, string entityName, string entityId, long? workspaceId = null, string? oldValues = null, string? newValues = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(action) ||
                string.IsNullOrWhiteSpace(entityName) ||
                string.IsNullOrWhiteSpace(entityId))
            {
                return;
            }

            // BR-AUD-01: recover the actor's WorkspaceMember identity whenever possible.
            // A workspace-scoped action with a known actor resolves to their current member row
            // (even after role changes). Removed members leave WorkspaceMemberId null so the
            // row still exists but is not falsely attributed to a later re-invited account.
            long? actorMemberId = null;
            if (workspaceId.HasValue && !string.IsNullOrWhiteSpace(userId))
            {
                actorMemberId = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                    .Where(wm => wm.WorkspaceId == workspaceId.Value && wm.UserId == userId)
                    .Select(wm => (long?)wm.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var log = new AuditLog
            {
                WorkspaceId = workspaceId ?? 0,
                WorkspaceMemberId = actorMemberId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues
            };

            _logger.LogInformation("Audit log created. Action: {Action}, Entity: {Entity}, EntityId: {EntityId}, WorkspaceId: {WorkspaceId}, ActorMemberId: {ActorMemberId}",
                    action,
                    entityName,
                    entityId,
                    workspaceId,
                    actorMemberId);
            await _unitOfWork.AuditLogs.AddAsync(log, cancellationToken);
        }
    }
}
