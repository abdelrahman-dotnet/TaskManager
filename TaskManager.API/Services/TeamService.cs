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

        public TeamService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            IMembershipService membershipService,
            IMapper mapper,
            ILogger<TeamService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _membershipService = membershipService;
            _mapper = mapper;
            _logger = logger;
        }

        // MEMBERSHIP: filters to teams the user belongs to, as an IN subquery against
        // TeamMembers. No bypass - see ITeamService.cs's comment.
        public async Task<PagedResult<TeamReadDto>> GetAllAsync(TeamQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Teams.GetAllQuery().AsNoTracking();

            var memberTeamIds = _unitOfWork.TeamMembers.GetAllQuery()
                .Where(tm => tm.UserId == currentUserId)
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
                t => t.Manager,
                t => t.Members,
                t => t.Projects);

            if (team == null)
            {
                _logger.LogWarning("GetTeamById failed. Team not found. TeamId: {TeamId}", id);
                throw new NotFoundException("Team not found.");
            }

            var canAccess = await _membershipService.CanAccessTeamAsync(id, currentUserId, cancellationToken);
            if (!canAccess)
            {
                _logger.LogWarning("GetTeamById forbidden (Membership). UserId: {UserId}, TeamId: {TeamId}", currentUserId, id);
                throw new ForbiddenException("You are not a member of this team.");
            }

            return _mapper.Map<TeamReadDto>(team);
        }

        public async Task<TeamReadDto> CreateAsync(TeamCreateDto dto, string managerId, CancellationToken cancellationToken = default)
        {
            var team = _mapper.Map<Team>(dto);
            team.ManagerId = managerId;

            await _unitOfWork.Teams.AddAsync(team, cancellationToken);
            // Save first - Team.Id is DB-generated, so it isn't known until after this completes.
            await _unitOfWork.CompleteAsync(cancellationToken);

            // MEMBERSHIP: creating the first Owner is this method's job, not
            // IMembershipService.AddTeamMemberAsync's - it goes in directly, bypassing that
            // method entirely (which requires an existing Owner/Manager to authorize the add -
            // there isn't one yet for a brand-new team). See MembershipService's comments.
            var ownerMembership = new TeamMember
            {
                TeamId = team.Id,
                UserId = managerId,
                Role = MembershipRole.Owner
            };
            await _unitOfWork.TeamMembers.AddAsync(ownerMembership, cancellationToken);

            var newValues = JsonSerializer.Serialize(new { team.Name, team.Description, team.ManagerId });
            await _auditLogService.LogAsync(managerId, "Create Team", nameof(Team), team.Id.ToString(), newValues: newValues);
            // Second save - persists the TeamMember(Owner) and the audit row together.
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Team created successfully. TeamId: {TeamId}, UserId: {CurrentUserId}", team.Id, managerId);
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

            // MEMBERSHIP: Permission (TeamsUpdate, checked at the Controller) && Membership
            // (must be Owner/Manager of THIS team) - both have to succeed.
            await _membershipService.EnsureCanManageTeamAsync(id, currentUserId, cancellationToken);

            if (!string.Equals(dto.ManagerId, team.ManagerId, StringComparison.Ordinal))
            {
                var manager = await _userManager.FindByIdAsync(dto.ManagerId);
                if (manager is null)
                    throw new NotFoundException("Manager not found.");
            }

            var oldValues = JsonSerializer.Serialize(new { team.Name, team.Description, team.ManagerId });

            _mapper.Map(dto, team);
            team.UpdatedAt = DateTime.UtcNow;

            var newValues = JsonSerializer.Serialize(new { team.Name, team.Description, team.ManagerId });

            _unitOfWork.Teams.Update(team);

            // Id already known - Audit is staged before the single save.
            await _auditLogService.LogAsync(currentUserId, "Update", "Team", id.ToString(), oldValues, newValues);

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

            // MEMBERSHIP: same as UpdateAsync above - Permission && Membership.
            await _membershipService.EnsureCanManageTeamAsync(id, currentUserId, cancellationToken);

            var oldValues = JsonSerializer.Serialize(new { team.Name, team.Description, team.ManagerId });

            _unitOfWork.Teams.Delete(team);

            await _auditLogService.LogAsync(currentUserId, "Delete", "Team", id.ToString(), oldValues: oldValues);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Team deleted successfully. TeamId: {TeamId}, UserId: {CurrentUserId}", id, currentUserId);
        }
    }
}
