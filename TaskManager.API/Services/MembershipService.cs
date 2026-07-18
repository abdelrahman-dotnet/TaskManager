using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Exceptions;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<MembershipService> _logger;

        public MembershipService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            ILogger<MembershipService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // ══════════════════════════════ Access Checks ══════════════════════════════

        public async Task<bool> IsTeamMemberAsync(long teamId, string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.TeamMembers.ExistsAsync(
                tm => tm.TeamId == teamId && tm.UserId == userId,
                cancellationToken);
        }

        public async Task<bool> IsProjectMemberAsync(long projectId, string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ProjectMembers.ExistsAsync(
                pm => pm.ProjectId == projectId && pm.UserId == userId,
                cancellationToken);
        }

        public async Task<bool> CanAccessTeamAsync(long teamId, string userId, CancellationToken cancellationToken = default)
        {
            return await IsTeamMemberAsync(teamId, userId, cancellationToken);
        }

        public async Task<bool> CanAccessProjectAsync(long projectId, string userId, CancellationToken cancellationToken = default)
        {
            return await IsProjectMemberAsync(projectId, userId, cancellationToken);
        }

        public async Task<bool> CanAccessTaskAsync(long taskId, string userId, CancellationToken cancellationToken = default)
        {
            // Composite: Task -> Project -> ProjectMember. If the task doesn't exist at all,
            // this returns false rather than throwing - "can this user access it" is a yes/no
            // question here; NotFound is the calling Service's call to make, not this one's.
            var projectId = await _unitOfWork.Tasks.GetAllQuery()
                .Where(t => t.Id == taskId)
                .Select(t => (long?)t.ProjectId)
                .FirstOrDefaultAsync(cancellationToken);

            if (projectId == null)
                return false;

            return await IsProjectMemberAsync(projectId.Value, userId, cancellationToken);
        }

        // ══════════════════════════════ Role Checks ═════════════════════════════════

        public async Task<MembershipRole?> GetUserTeamRoleAsync(long teamId, string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.TeamMembers.GetAllQuery()
                .Where(tm => tm.TeamId == teamId && tm.UserId == userId)
                .Select(tm => (MembershipRole?)tm.Role)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<MembershipRole?> GetUserProjectRoleAsync(long projectId, string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
                .Select(pm => (MembershipRole?)pm.Role)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> IsTeamOwnerAsync(long teamId, string userId, CancellationToken cancellationToken = default)
        {
            var role = await GetUserTeamRoleAsync(teamId, userId, cancellationToken);
            return role == MembershipRole.Owner;
        }

        public async Task<bool> IsProjectOwnerAsync(long projectId, string userId, CancellationToken cancellationToken = default)
        {
            var role = await GetUserProjectRoleAsync(projectId, userId, cancellationToken);
            return role == MembershipRole.Owner;
        }

        public async Task EnsureCanManageTeamAsync(long teamId, string userId, CancellationToken cancellationToken = default)
        {
            var role = await GetUserTeamRoleAsync(teamId, userId, cancellationToken);
            if (role != MembershipRole.Owner && role != MembershipRole.Manager)
            {
                _logger.LogWarning("EnsureCanManageTeam forbidden. TeamId: {TeamId}, UserId: {UserId}", teamId, userId);
                throw new ForbiddenException("You must be a Team Owner or Manager to perform this action.");
            }
        }

        public async Task EnsureCanManageProjectAsync(long projectId, string userId, CancellationToken cancellationToken = default)
        {
            var role = await GetUserProjectRoleAsync(projectId, userId, cancellationToken);
            if (role != MembershipRole.Owner && role != MembershipRole.Manager)
            {
                _logger.LogWarning("EnsureCanManageProject forbidden. ProjectId: {ProjectId}, UserId: {UserId}", projectId, userId);
                throw new ForbiddenException("You must be a Project Owner or Manager to perform this action.");
            }
        }

        // ══════════════════════════════ Listings ════════════════════════════════════

        public async Task<IEnumerable<TeamMember>> GetTeamMembersAsync(long teamId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.TeamMembers.GetAllQuery()
                .AsNoTracking()
                .Where(tm => tm.TeamId == teamId)
                .Include(tm => tm.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(long projectId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ProjectMembers.GetAllQuery()
                .AsNoTracking()
                .Where(pm => pm.ProjectId == projectId)
                .Include(pm => pm.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TeamMember>> GetUserTeamMembershipsAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.TeamMembers.GetAllQuery()
                .AsNoTracking()
                .Where(tm => tm.UserId == userId)
                .Include(tm => tm.Team)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProjectMember>> GetUserProjectMembershipsAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ProjectMembers.GetAllQuery()
                .AsNoTracking()
                .Where(pm => pm.UserId == userId)
                .Include(pm => pm.Project)
                .ToListAsync(cancellationToken);
        }

        // ══════════════════════════════ Mutations ═══════════════════════════════════

        public async Task AddTeamMemberAsync(long teamId, string userId, MembershipRole role, string currentUserId, CancellationToken cancellationToken = default)
        {
            // This method is only for adding a member to an EXISTING team - creating the
            // first Owner is TeamService.CreateAsync's job, not this one's (see the flow:
            // Create Team -> Create TeamMember(Owner) directly, bypassing this method
            // entirely). So there's no bootstrap exception here - every call goes through
            // the same Ownership/Permission Check as every other mutation.
            await EnsureCanManageTeamAsync(teamId, currentUserId, cancellationToken);

            var teamExists = await _unitOfWork.Teams.ExistsAsync(t => t.Id == teamId, cancellationToken);
            if (!teamExists)
                throw new NotFoundException("Team not found.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found.");

            var alreadyMember = await IsTeamMemberAsync(teamId, userId, cancellationToken);
            if (alreadyMember)
                throw new ConflictException("This user is already a member of the team.");

            var member = new TeamMember
            {
                TeamId = teamId,
                UserId = userId,
                Role = role
            };

            await _unitOfWork.TeamMembers.AddAsync(member, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new { TeamId = teamId, UserId = userId, Role = role });
            await _auditLogService.LogAsync(currentUserId, "Add Team Member", nameof(TeamMember), member.Id.ToString(), null, newValues);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Team member added. TeamId: {TeamId}, UserId: {UserId}, Role: {Role}, By: {CurrentUserId}",
                teamId, userId, role, currentUserId);
        }

        public async Task RemoveTeamMemberAsync(long teamId, string userId, string currentUserId, CancellationToken cancellationToken = default)
        {
            await EnsureCanManageTeamAsync(teamId, currentUserId, cancellationToken);

            var member = await _unitOfWork.TeamMembers.GetAllQuery()
                .Where(tm => tm.TeamId == teamId && tm.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
                throw new NotFoundException("This user is not a member of the team.");

            var oldValues = JsonSerializer.Serialize(new { TeamId = teamId, UserId = userId, member.Role });

            _unitOfWork.TeamMembers.Delete(member);

            await _auditLogService.LogAsync(currentUserId, "Remove Team Member", nameof(TeamMember), member.Id.ToString(), oldValues, null);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Team member removed. TeamId: {TeamId}, UserId: {UserId}, By: {CurrentUserId}",
                teamId, userId, currentUserId);
        }

        public async Task ChangeTeamMemberRoleAsync(long teamId, string userId, MembershipRole newRole, string currentUserId, CancellationToken cancellationToken = default)
        {
            await EnsureCanManageTeamAsync(teamId, currentUserId, cancellationToken);

            var member = await _unitOfWork.TeamMembers.GetAllQuery()
                .Where(tm => tm.TeamId == teamId && tm.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
                throw new NotFoundException("This user is not a member of the team.");

            var oldValues = JsonSerializer.Serialize(new { member.Role });

            member.Role = newRole;
            member.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.TeamMembers.Update(member);

            var newValues = JsonSerializer.Serialize(new { Role = newRole });
            await _auditLogService.LogAsync(currentUserId, "Change Team Member Role", nameof(TeamMember), member.Id.ToString(), oldValues, newValues);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Team member role changed. TeamId: {TeamId}, UserId: {UserId}, NewRole: {NewRole}, By: {CurrentUserId}",
                teamId, userId, newRole, currentUserId);
        }

        public async Task AddProjectMemberAsync(long projectId, string userId, MembershipRole role, string currentUserId, CancellationToken cancellationToken = default)
        {
            // Same reasoning as AddTeamMemberAsync - creating the first Owner is
            // ProjectService.CreateAsync's job, not this method's.
            await EnsureCanManageProjectAsync(projectId, currentUserId, cancellationToken);

            var projectExists = await _unitOfWork.Projects.ExistsAsync(p => p.Id == projectId, cancellationToken);
            if (!projectExists)
                throw new NotFoundException("Project not found.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found.");

            var alreadyMember = await IsProjectMemberAsync(projectId, userId, cancellationToken);
            if (alreadyMember)
                throw new ConflictException("This user is already a member of the project.");

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = role
            };

            await _unitOfWork.ProjectMembers.AddAsync(member, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new { ProjectId = projectId, UserId = userId, Role = role });
            await _auditLogService.LogAsync(currentUserId, "Add Project Member", nameof(ProjectMember), member.Id.ToString(), null, newValues);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Project member added. ProjectId: {ProjectId}, UserId: {UserId}, Role: {Role}, By: {CurrentUserId}",
                projectId, userId, role, currentUserId);
        }

        public async Task RemoveProjectMemberAsync(long projectId, string userId, string currentUserId, CancellationToken cancellationToken = default)
        {
            await EnsureCanManageProjectAsync(projectId, currentUserId, cancellationToken);

            var member = await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
                throw new NotFoundException("This user is not a member of the project.");

            var oldValues = JsonSerializer.Serialize(new { ProjectId = projectId, UserId = userId, member.Role });

            _unitOfWork.ProjectMembers.Delete(member);

            await _auditLogService.LogAsync(currentUserId, "Remove Project Member", nameof(ProjectMember), member.Id.ToString(), oldValues, null);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Project member removed. ProjectId: {ProjectId}, UserId: {UserId}, By: {CurrentUserId}",
                projectId, userId, currentUserId);
        }

        public async Task ChangeProjectMemberRoleAsync(long projectId, string userId, MembershipRole newRole, string currentUserId, CancellationToken cancellationToken = default)
        {
            await EnsureCanManageProjectAsync(projectId, currentUserId, cancellationToken);

            var member = await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
                throw new NotFoundException("This user is not a member of the project.");

            var oldValues = JsonSerializer.Serialize(new { member.Role });

            member.Role = newRole;
            member.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.ProjectMembers.Update(member);

            var newValues = JsonSerializer.Serialize(new { Role = newRole });
            await _auditLogService.LogAsync(currentUserId, "Change Project Member Role", nameof(ProjectMember), member.Id.ToString(), oldValues, newValues);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Project member role changed. ProjectId: {ProjectId}, UserId: {UserId}, NewRole: {NewRole}, By: {CurrentUserId}",
                projectId, userId, newRole, currentUserId);
        }
    }
}
