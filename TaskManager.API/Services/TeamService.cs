using TaskManager.API.Exceptions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskManager.API.Config;
using TaskManager.API.Config.FiltersConfigs;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.DTOs.Team;
using TaskManager.API.Extentions;
using TaskManager.API.Helpers;
using TaskManager.Bussiness.Authorization;
using TaskManager.Bussiness.Authorization.ResourceConditions;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly IMembershipService _membershipService;
        private readonly IMapper _mapper;
        private readonly ILogger<TeamService> _logger;
        private readonly IWorkspaceAuthorizationService _authService;

        public TeamService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            IMembershipService membershipService,
            IMapper mapper,
            ILogger<TeamService> logger,
            IWorkspaceAuthorizationService authService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _membershipService = membershipService;
            _mapper = mapper;
            _logger = logger;
            _authService = authService;
        }

        // RESOLVE: Team -> Workspace for the Authorization Pipeline (Visibility).
        private async Task<long> ResolveTeamWorkspaceAsync(long teamId, CancellationToken cancellationToken)
        {
            var workspaceId = await _unitOfWork.Teams.GetAllQuery()
                .Where(t => t.Id == teamId)
                .Select(t => (long?)t.WorkspaceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (workspaceId == null)
            {
                _logger.LogWarning("Team not found or has no workspace. TeamId: {TeamId}", teamId);
                throw new NotFoundException("Team not found.");
            }

            return workspaceId.Value;
        }

        // PIPELINE FAILURE HANDLING: Visibility (NotFound) -> 404, everything else -> 403.
        private void ThrowIfFailed(AuthorizationResult authResult, long resourceId, string userId, string op)
        {
            _logger.LogWarning(
                "{Op} pipeline failed. Reason: {Reason}, UserId: {UserId}, TeamId: {TeamId}",
                op, authResult.FailureReason, userId, resourceId);

            throw authResult.FailureReason == AuthorizationFailureReason.NotFound
                ? new NotFoundException(authResult.Message)
                : new ForbiddenException(authResult.Message);
        }

        // VISIBILITY (read-listing): filters to teams the user belongs to, as an IN
        // subquery against TeamMembers. No single workspaceId for a cross-workspace
        // list, so read-listing keeps the membership-scoped query rather than the
        // per-workspace pipeline.
        public async Task<PagedResult<TeamReadDto>> GetAllAsync(TeamQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Teams.GetAllQuery().AsNoTracking();

            var memberTeamIds = _unitOfWork.TeamMembers.GetAllQuery()
                .Where(tm => tm.WorkspaceMember.UserId == currentUserId)
                .Select(tm => tm.TeamId);

            query = query.Where(t => memberTeamIds.Contains(t.Id));

            query = query.ApplyFiltering(queryParams, TeamFilterConfig.map);

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var search = queryParams.Search;
                query = query.Where(t => t.Name.Contains(search));
            }

            query = query.ApplySorting(queryParams.Sorts, AllowedSortingFields.Teams);

            var projected = query.ProjectTo<TeamReadDto>(_mapper.ConfigurationProvider);
            var result = await projected.ToPagedResultAsync(queryParams.Page, queryParams.PageSize, cancellationToken);

            _logger.LogInformation("Teams retrieved successfully. Count: {Count}", result.Data.Count);
            return result;
        }

        public async Task<TeamReadDto> GetByIdAsync(long id, string currentUserId, CancellationToken cancellationToken = default)
        {
            var team = await _unitOfWork.Teams.FirstOrDefaultAsync(
                t => t.Id == id,
                cancellationToken,
                t => t.TeamMembers,
                t => t.ProjectTeams,
                t => t.ProjectTeams.Select(pt => pt.Team));

            if (team == null)
            {
                _logger.LogWarning("GetTeamById failed. Team not found. TeamId: {TeamId}", id);
                throw new NotFoundException("Team not found.");
            }

            // TEAM PIVOT: Team -> Workspace for pipeline stage 1 (Visibility).
            var workspaceId = await ResolveTeamWorkspaceAsync(id, cancellationToken);

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.WorkspaceView);

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, id, currentUserId, "GetTeamById");

            return _mapper.Map<TeamReadDto>(team);
        }

        // PIPELINE (Auth Pipeline): Visibility -> Permission (Teams.Create) -> Operation.
        // The controller passes the target WorkspaceId (create DTOs carry none - the route
        // is POST /api/teams/{workspaceId}).
        public async Task<TeamReadDto> CreateAsync(TeamCreateDto dto, long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TeamsCreate);

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, workspaceId, currentUserId, "CreateTeam");

            var team = _mapper.Map<Team>(dto);
            team.WorkspaceId = workspaceId; // DTOs carry none; fixes the legacy WorkspaceId=0 FK bug.

            await _unitOfWork.Teams.AddAsync(team, cancellationToken);
            // Save first - Team.Id is DB-generated, so it isn't known until after this completes.
            await _unitOfWork.CompleteAsync(cancellationToken);

            // RESOLVE workspace membership for the user who will be the team's Owner.
            var ownerWm = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.UserId == currentUserId && wm.WorkspaceId == team.WorkspaceId)
                .FirstOrDefaultAsync(cancellationToken);
            if (ownerWm is null)
            {
                _logger.LogWarning("CreateTeam failed. Caller is not a member of the workspace. UserId: {UserId}, WorkspaceId: {WorkspaceId}", currentUserId, team.WorkspaceId);
                throw new NotFoundException("You must be a member of this workspace to create a team.");
            }

            var ownerMembership = new TeamMember
            {
                TeamId = team.Id,
                WorkspaceMemberId = ownerWm.Id
            };
            await _unitOfWork.TeamMembers.AddAsync(ownerMembership, cancellationToken);

            var newValues = JsonSerializer.Serialize(new { team.Name, team.Description });
            await _auditLogService.LogAsync(currentUserId, "Create Team", nameof(Team), team.Id.ToString(), workspaceId: workspaceId, newValues: newValues);
            // Second save - persists the TeamMember(Owner) and the audit row together.
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Team created successfully. TeamId: {TeamId}, UserId: {CurrentUserId}", team.Id, currentUserId);
            return _mapper.Map<TeamReadDto>(team);
        }

        public async Task<TeamReadDto> UpdateAsync(long id, TeamUpdateDto dto, string currentUserId, CancellationToken cancellationToken = default)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(id, cancellationToken);
            if (team == null)
            {
                _logger.LogWarning("UpdateTeam failed. Team not found. TeamId: {TeamId}", id);
                throw new NotFoundException("Team not found.");
            }

            var workspaceId = team.WorkspaceId;

            // Authorization Pipeline: Visibility -> Permission (Teams.Update).
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TeamsUpdate);

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, id, currentUserId, "UpdateTeam");

            var oldValues = JsonSerializer.Serialize(new { team.Name, team.Description });

            _mapper.Map(dto, team);
            team.UpdatedAt = DateTime.UtcNow;

            var newValues = JsonSerializer.Serialize(new { team.Name, team.Description });

            _unitOfWork.Teams.Update(team);

            // Id already known - Audit is staged before the single save.
            await _auditLogService.LogAsync(currentUserId, "Update", "Team", id.ToString(), workspaceId: workspaceId, oldValues: oldValues, newValues: newValues);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Team updated successfully. TeamId: {TeamId},UserId: {CurrentUserId}", id, currentUserId);
            return _mapper.Map<TeamReadDto>(team);
        }

        public async Task DeleteAsync(long id, string currentUserId, CancellationToken cancellationToken = default)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(id, cancellationToken);
            if (team == null)
            {
                _logger.LogWarning("DeleteTeam failed. Team not found. TeamId: {TeamId}", id);
                throw new NotFoundException("Team not found.");
            }

            var workspaceId = team.WorkspaceId;

            // Authorization Pipeline: Visibility -> Permission (Teams.Delete).
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.TeamsDelete);

            if (!authResult.Succeeded)
                ThrowIfFailed(authResult, id, currentUserId, "DeleteTeam");

            var oldValues = JsonSerializer.Serialize(new { team.Name, team.Description });

            _unitOfWork.Teams.Delete(team);

            await _auditLogService.LogAsync(currentUserId, "Delete", "Team", id.ToString(), workspaceId: workspaceId, oldValues: oldValues);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Team deleted successfully. TeamId: {TeamId}, UserId: {CurrentUserId}", id, currentUserId);
        }
    }
}
