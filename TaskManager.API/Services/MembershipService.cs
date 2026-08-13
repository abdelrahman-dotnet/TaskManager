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
                tm => tm.TeamId == teamId && tm.WorkspaceMember.UserId == userId,
                cancellationToken);
        }

        public async Task<bool> IsProjectMemberAsync(long projectId, string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ProjectMembers.ExistsAsync(
                pm => pm.ProjectId == projectId && pm.WorkspaceMember.UserId == userId,
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• Role Checks â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Authority flows solely from WorkspaceMember.Role (no TeamRole/ProjectRole).

        public async Task<WorkspaceRole?> GetUserTeamRoleAsync(long teamId, string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.TeamMembers.GetAllQuery()
                .Where(tm => tm.TeamId == teamId && tm.WorkspaceMember.UserId == userId)
                .Select(tm => (WorkspaceRole?)tm.WorkspaceMember.Role)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<WorkspaceRole?> GetUserProjectRoleAsync(long projectId, string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == projectId && pm.WorkspaceMember.UserId == userId)
                .Select(pm => (WorkspaceRole?)pm.WorkspaceMember.Role)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> IsTeamOwnerAsync(long teamId, string userId, CancellationToken cancellationToken = default)
        {
            var role = await GetUserTeamRoleAsync(teamId, userId, cancellationToken);
            return role == WorkspaceRole.Owner;
        }

        public async Task<bool> IsProjectOwnerAsync(long projectId, string userId, CancellationToken cancellationToken = default)
        {
            var role = await GetUserProjectRoleAsync(projectId, userId, cancellationToken);
            return role == WorkspaceRole.Owner;
        }

        public async Task EnsureCanManageTeamAsync(long teamId, string userId, CancellationToken cancellationToken = default)
        {
            var role = await GetUserTeamRoleAsync(teamId, userId, cancellationToken);
            if (role != WorkspaceRole.Owner && role != WorkspaceRole.Admin)
            {
                _logger.LogWarning("EnsureCanManageTeam forbidden. TeamId: {TeamId}, UserId: {UserId}", teamId, userId);
                throw new ForbiddenException("You must be a Team Owner or Admin to perform this action.");
            }
        }

        public async Task EnsureCanManageProjectAsync(long projectId, string userId, CancellationToken cancellationToken = default)
        {
            var role = await GetUserProjectRoleAsync(projectId, userId, cancellationToken);
            if (role != WorkspaceRole.Owner && role != WorkspaceRole.Admin)
            {
                _logger.LogWarning("EnsureCanManageProject forbidden. ProjectId: {ProjectId}, UserId: {UserId}", projectId, userId);
                throw new ForbiddenException("You must be a Project Owner or Admin to perform this action.");
            }
        }
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• Listings â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• Listings â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        public async Task<IEnumerable<TeamMember>> GetTeamMembersAsync(long teamId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var canAccess = await CanAccessTeamAsync(teamId, currentUserId, cancellationToken);
            if (!canAccess)
            {
                _logger.LogWarning("GetTeamMembers forbidden. UserId: {UserId}, TeamId: {TeamId}", currentUserId, teamId);
                throw new ForbiddenException("You are not a member of this team.");
            }

            return await _unitOfWork.TeamMembers.GetAllQuery()
                .AsNoTracking()
                .Where(tm => tm.TeamId == teamId)
                .Include(tm => tm.WorkspaceMember)
                .ThenInclude(wm => wm.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(long projectId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var canAccess = await CanAccessProjectAsync(projectId, currentUserId, cancellationToken);
            if (!canAccess)
            {
                _logger.LogWarning("GetProjectMembers forbidden. UserId: {UserId}, ProjectId: {ProjectId}", currentUserId, projectId);
                throw new ForbiddenException("You are not a member of this project.");
            }

            return await _unitOfWork.ProjectMembers.GetAllQuery()
                .AsNoTracking()
                .Where(pm => pm.ProjectId == projectId)
                .Include(pm => pm.WorkspaceMember)
                .ThenInclude(wm => wm.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TeamMember>> GetUserTeamMembershipsAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.TeamMembers.GetAllQuery()
                .AsNoTracking()
                .Where(tm => tm.WorkspaceMember.UserId == userId)
                .Include(tm => tm.Team)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProjectMember>> GetUserProjectMembershipsAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ProjectMembers.GetAllQuery()
                .AsNoTracking()
                .Where(pm => pm.WorkspaceMember.UserId == userId)
                .Include(pm => pm.Project)
                .ToListAsync(cancellationToken);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• Mutations â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        public async Task AddTeamMemberAsync(long teamId, string userId, WorkspaceRole role, string currentUserId, CancellationToken cancellationToken = default)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId, cancellationToken);
            if (team is null)
                throw new NotFoundException("Team not found.");

            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser is null)
                throw new NotFoundException("User not found.");

            var teamWorkspaceMember = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.WorkspaceId == team.WorkspaceId && wm.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
            if (teamWorkspaceMember is null)
                throw new BadRequestException("The user must be a member of this workspace before joining a team.");

            var alreadyMember = await IsTeamMemberAsync(teamId, userId, cancellationToken);
            if (alreadyMember)
                throw new ConflictException("This user is already a member of the team.");

            // Adding to a team confirms/upgrades the user's workspace role (roles are workspace-scoped).
            if (teamWorkspaceMember.Role < role)
            {
                teamWorkspaceMember.Role = role;
                teamWorkspaceMember.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.WorkspaceMembers.Update(teamWorkspaceMember);
            }

            var member = new TeamMember
            {
                TeamId = teamId,
                WorkspaceMemberId = teamWorkspaceMember.Id
            };

            await _unitOfWork.TeamMembers.AddAsync(member, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new { TeamId = teamId, UserId = userId, Role = role });
            await _auditLogService.LogAsync(currentUserId, "Add Team Member", nameof(TeamMember), member.Id.ToString(), null, newValues, cancellationToken);

            _logger.LogInformation("Team member added. TeamId: {TeamId}, UserId: {UserId}, Role: {Role}, By: {CurrentUserId}",
                teamId, userId, role, currentUserId);
        }

        public async Task AddProjectMemberAsync(long projectId, string userId, WorkspaceRole role, string currentUserId, CancellationToken cancellationToken = default)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
            if (project is null)
                throw new NotFoundException("Project not found.");

            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser is null)
                throw new NotFoundException("User not found.");

            var projectWorkspaceMember = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.WorkspaceId == project.WorkspaceId && wm.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
            if (projectWorkspaceMember is null)
                throw new BadRequestException("The user must be a member of this workspace before joining a project.");

            var alreadyMember = await IsProjectMemberAsync(projectId, userId, cancellationToken);
            if (alreadyMember)
                throw new ConflictException("This user is already a member of the project.");

            // Adding to a project confirms/upgrades the user's workspace role (roles are workspace-scoped).
            if (projectWorkspaceMember.Role < role)
            {
                projectWorkspaceMember.Role = role;
                projectWorkspaceMember.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.WorkspaceMembers.Update(projectWorkspaceMember);
            }

            var member = new ProjectMember
            {
                ProjectId = projectId,
                WorkspaceMemberId = projectWorkspaceMember.Id
            };

            await _unitOfWork.ProjectMembers.AddAsync(member, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new { ProjectId = projectId, UserId = userId, Role = role });
            await _auditLogService.LogAsync(currentUserId, "Add Project Member", nameof(ProjectMember), member.Id.ToString(), null, newValues, cancellationToken);

            _logger.LogInformation("Project member added. ProjectId: {ProjectId}, UserId: {UserId}, Role: {Role}, By: {CurrentUserId}",
                projectId, userId, role, currentUserId);
        }

        public async Task RemoveTeamMemberAsync(long teamId, string userId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var member = await _unitOfWork.TeamMembers.GetAllQuery()
                .Where(tm => tm.TeamId == teamId && tm.WorkspaceMember.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
                throw new NotFoundException("This user is not a member of the team.");

            var oldValues = JsonSerializer.Serialize(new { TeamId = teamId, UserId = userId, WorkspaceRole = member.WorkspaceMember.Role });

            _unitOfWork.TeamMembers.Delete(member);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(currentUserId, "Remove Team Member", nameof(TeamMember), member.Id.ToString(), oldValues, null, cancellationToken);

            _logger.LogInformation("Team member removed. TeamId: {TeamId}, UserId: {UserId}, By: {CurrentUserId}",
                teamId, userId, currentUserId);
        }

        public async Task RemoveProjectMemberAsync(long projectId, string userId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var member = await _unitOfWork.ProjectMembers.GetAllQuery()
                .Where(pm => pm.ProjectId == projectId && pm.WorkspaceMember.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (member == null)
                throw new NotFoundException("This user is not a member of the project.");

            var oldValues = JsonSerializer.Serialize(new { ProjectId = projectId, UserId = userId, WorkspaceRole = member.WorkspaceMember.Role });

            _unitOfWork.ProjectMembers.Delete(member);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditLogService.LogAsync(currentUserId, "Remove Project Member", nameof(ProjectMember), member.Id.ToString(), oldValues, null, cancellationToken);

            _logger.LogInformation("Project member removed. ProjectId: {ProjectId}, UserId: {UserId}, By: {CurrentUserId}",
                projectId, userId, currentUserId);
        }

        // The ONLY role-change operation: roles are workspace-scoped
        // (Owner/Admin/Member). The caller must already be Owner/Admin of the workspace.
        public async Task ChangeWorkspaceMemberRoleAsync(long workspaceId, string userId, WorkspaceRole newRole, string currentUserId, CancellationToken cancellationToken = default)
        {
            var ws = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId, cancellationToken);
            if (ws is null)
                throw new NotFoundException("Workspace not found.");

            await EnsureCanManageWorkspaceAsync(workspaceId, currentUserId, cancellationToken);

            var wm = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(x => x.WorkspaceId == workspaceId && x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
            if (wm is null)
                throw new NotFoundException("This user is not a member of this workspace.");

            if (wm.Role == newRole)
                return;

            var oldValues = JsonSerializer.Serialize(new { WorkspaceId = workspaceId, UserId = userId, WorkspaceRole = wm.Role });

            wm.Role = newRole;
            wm.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.WorkspaceMembers.Update(wm);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new { WorkspaceRole = newRole });
            await _auditLogService.LogAsync(currentUserId, "Change Workspace Member Role", nameof(WorkspaceMember), wm.Id.ToString(), oldValues, newValues, cancellationToken);

            _logger.LogInformation("Workspace member role changed. WorkspaceId: {WorkspaceId}, UserId: {UserId}, NewRole: {NewRole}, By: {CurrentUserId}",
                workspaceId, userId, newRole, currentUserId);
        }

        // Throws ForbiddenException if the user isn't Owner/Admin of the Workspace.
        public async Task EnsureCanManageWorkspaceAsync(long workspaceId, string userId, CancellationToken cancellationToken = default)
        {
            var wm = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(x => x.WorkspaceId == workspaceId && x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            var role = wm?.Role;
            if (role != WorkspaceRole.Owner && role != WorkspaceRole.Admin)
            {
                _logger.LogWarning("EnsureCanManageWorkspace failed. User is not Owner/Admin of workspace. UserId: {UserId}, WorkspaceId: {WorkspaceId}", userId, workspaceId);
                throw new ForbiddenException("Only the Workspace Owner or Admin can perform this action.");
            }
        }
    }
}
