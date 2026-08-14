using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Exceptions;
using TaskManager.API.Extentions;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Authorization;
using TaskManager.Bussiness.Authorization.ResourceConditions;
using TaskManager.Data.Entities;

namespace TaskManager.API.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<MembershipService> _logger;
        private readonly IWorkspaceAuthorizationService _authService;

        public MembershipService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IAuditLogService auditLogService,
            ILogger<MembershipService> logger,
            IWorkspaceAuthorizationService authService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _logger = logger;
            _authService = authService;
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

        // Authorization Pipeline: Visibility -> Permission (Teams.ManageMembers).
        public async Task EnsureCanManageTeamAsync(long teamId, string userId, CancellationToken cancellationToken = default)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId, cancellationToken);
            if (team is null)
                throw new NotFoundException("Team not found.");

            var authResult = await _authService.AuthorizeAsync(
                team.WorkspaceId,
                userId,
                Permissions.TeamsManageMembers,
                null);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("EnsureCanManageTeam pipeline failed. Reason: {Reason}, TeamId: {TeamId}, UserId: {UserId}",
                    authResult.FailureReason, teamId, userId);
                throw authResult.ToAuthorizationException();
            }
        }

        // Authorization Pipeline: Visibility -> Permission (Projects.ManageMembers).
        public async Task EnsureCanManageProjectAsync(long projectId, string userId, CancellationToken cancellationToken = default)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
            if (project is null)
                throw new NotFoundException("Project not found.");

            var authResult = await _authService.AuthorizeAsync(
                project.WorkspaceId,
                userId,
                Permissions.ProjectsManageMembers,
                null);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("EnsureCanManageProject pipeline failed. Reason: {Reason}, ProjectId: {ProjectId}, UserId: {UserId}",
                    authResult.FailureReason, projectId, userId);
                throw authResult.ToAuthorizationException();
            }
        }
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• Listings â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â• Listings â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        // PIPELINE (Auth Pipeline): Visibility -> Permission (WorkspaceView for reads).
        public async Task<IEnumerable<TeamMember>> GetTeamMembersAsync(long teamId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId, cancellationToken);
            if (team is null)
            {
                _logger.LogWarning("GetTeamMembers failed. Team not found. TeamId: {TeamId}", teamId);
                throw new NotFoundException("Team not found.");
            }

            var authResult = await _authService.AuthorizeAsync(
                team.WorkspaceId,
                currentUserId,
                Permissions.WorkspaceView);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("GetTeamMembers pipeline failed. Reason: {Reason}, UserId: {UserId}, TeamId: {TeamId}", authResult.FailureReason, currentUserId, teamId);
                throw authResult.ToAuthorizationException();
            }

            return await _unitOfWork.TeamMembers.GetAllQuery()
                .AsNoTracking()
                .Where(tm => tm.TeamId == teamId)
                .Include(tm => tm.WorkspaceMember)
                .ThenInclude(wm => wm.User)
                .ToListAsync(cancellationToken);
        }

        // PIPELINE (Auth Pipeline): Visibility -> Permission (WorkspaceView for reads).
        public async Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(long projectId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
            if (project is null)
            {
                _logger.LogWarning("GetProjectMembers failed. Project not found. ProjectId: {ProjectId}", projectId);
                throw new NotFoundException("Project not found.");
            }

            var authResult = await _authService.AuthorizeAsync(
                project.WorkspaceId,
                currentUserId,
                Permissions.WorkspaceView);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("GetProjectMembers pipeline failed. Reason: {Reason}, UserId: {UserId}, ProjectId: {ProjectId}", authResult.FailureReason, currentUserId, projectId);
                throw authResult.ToAuthorizationException();
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
            await _auditLogService.LogAsync(currentUserId, "Add Team Member", nameof(TeamMember), member.Id.ToString(), workspaceId: team.WorkspaceId, oldValues: null, newValues: newValues, cancellationToken: cancellationToken);

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
            await _auditLogService.LogAsync(currentUserId, "Add Project Member", nameof(ProjectMember), member.Id.ToString(), workspaceId: project.WorkspaceId, oldValues: null, newValues: newValues, cancellationToken: cancellationToken);

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

            await _auditLogService.LogAsync(currentUserId, "Remove Team Member", nameof(TeamMember), member.Id.ToString(), workspaceId: teamId, oldValues: oldValues, newValues: null, cancellationToken: cancellationToken);

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

            await _auditLogService.LogAsync(currentUserId, "Remove Project Member", nameof(ProjectMember), member.Id.ToString(), workspaceId: projectId, oldValues: oldValues, newValues: null, cancellationToken: cancellationToken);

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

            // Authorization Pipeline: Visibility -> Permission (Members.ChangeRole).
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.MembersChangeRole,
                null);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("ChangeWorkspaceMemberRole pipeline failed. Reason: {Reason}, WorkspaceId: {WorkspaceId}, UserId: {CurrentUserId}",
                    authResult.FailureReason, workspaceId, currentUserId);
                throw authResult.ToAuthorizationException();
            }

            var wm = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(x => x.WorkspaceId == workspaceId && x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
            if (wm is null)
                throw new NotFoundException("This user is not a member of this workspace.");

            // Resource Condition (S-12): an Admin cannot change the role of an Owner or another Admin.
            var protectedCheck = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.MembersChangeRole,
                new MemberProtectedRoleCondition(wm));

            if (!protectedCheck.Succeeded)
            {
                _logger.LogWarning("ChangeWorkspaceMemberRole protected-role check failed. UserId: {CurrentUserId}, TargetUserId: {UserId}", currentUserId, userId);
                throw protectedCheck.ToAuthorizationException();
            }

            if (wm.Role == newRole)
                return;

            var oldValues = JsonSerializer.Serialize(new { WorkspaceId = workspaceId, UserId = userId, WorkspaceRole = wm.Role });

            wm.Role = newRole;
            wm.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.WorkspaceMembers.Update(wm);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var newValues = JsonSerializer.Serialize(new { WorkspaceRole = newRole });
            await _auditLogService.LogAsync(currentUserId, "Change Workspace Member Role", nameof(WorkspaceMember), wm.Id.ToString(), workspaceId: workspaceId, oldValues: oldValues, newValues: newValues, cancellationToken: cancellationToken);

            _logger.LogInformation("Workspace member role changed. WorkspaceId: {WorkspaceId}, UserId: {UserId}, NewRole: {NewRole}, By: {CurrentUserId}",
                workspaceId, userId, newRole, currentUserId);
        }

        // Throws ForbiddenException if the user isn't Owner/Admin of the Workspace.
        // Authorization Pipeline: Visibility -> Permission (Workspace.Update).
        public async Task EnsureCanManageWorkspaceAsync(long workspaceId, string userId, CancellationToken cancellationToken = default)
        {
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                userId,
                Permissions.WorkspaceUpdate,
                null);

                        if (!authResult.Succeeded)
            {
                _logger.LogWarning("EnsureCanManageWorkspace pipeline failed. Reason: {Reason}, UserId: {UserId}, WorkspaceId: {WorkspaceId}",
                    authResult.FailureReason, userId, workspaceId);
                throw authResult.ToAuthorizationException();
            }
        }

        // ══════════════════ Workspace Member Lifecycle ══════════════════

        // PIPELINE: Visibility -> Permission (Members.Remove) -> Condition
        // (MemberNotOwnerCondition) -> BR-MEM-03 (cleanup TaskAssignments).
        public async Task RemoveWorkspaceMemberAsync(long workspaceId, string targetUserId, string currentUserId, CancellationToken cancellationToken = default)
        {
            if (string.Equals(targetUserId, currentUserId, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("You cannot remove yourself from the workspace. Use Suspend or transfer ownership instead.");

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.MembersRemove,
                null);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("RemoveWorkspaceMember pipeline failed. Reason: {Reason}, WorkspaceId: {WorkspaceId}, CurrentUserId: {CurrentUserId}",
                    authResult.FailureReason, workspaceId, currentUserId);
                throw authResult.ToAuthorizationException();
            }

            var target = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.WorkspaceId == workspaceId && wm.UserId == targetUserId && !wm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (target is null)
                throw new NotFoundException("This user is not a member of this workspace.");

            if (target.Status == WorkspaceMemberStatus.Removed)
                throw new BadRequestException("This user has already been removed from the workspace.");

            // Resource Condition (S-13): cannot remove the workspace Owner.
            var protectedCheck = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.MembersRemove,
                new MemberNotOwnerCondition(target));

            if (!protectedCheck.Succeeded)
            {
                _logger.LogWarning("RemoveWorkspaceMember owner-protection failed. WorkspaceId: {WorkspaceId}, TargetUserId: {TargetUserId}", workspaceId, targetUserId);
                throw protectedCheck.ToAuthorizationException();
            }

            // BR-MEM-03: clean up task assignments held by the removed member.
            var assignments = await _unitOfWork.TaskAssignments.GetAllQuery()
                .Where(a => a.WorkspaceMemberId == target.Id)
                .ToListAsync(cancellationToken);

            foreach (var a in assignments)
            {
                _unitOfWork.TaskAssignments.Delete(a);
            }

            var oldValues = JsonSerializer.Serialize(new { WorkspaceId = workspaceId, UserId = targetUserId, Role = target.Role, Status = target.Status });

            // Soft-remove: marks the member Removed so history rows keep referential integrity.
            target.Status = WorkspaceMemberStatus.Removed;
            target.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.WorkspaceMembers.Update(target);

            await _auditLogService.LogAsync(currentUserId, "Remove Workspace Member", nameof(WorkspaceMember), target.Id.ToString(), workspaceId: workspaceId, oldValues: oldValues, newValues: null, cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Workspace member removed. WorkspaceId: {WorkspaceId}, TargetUserId: {TargetUserId}, By: {CurrentUserId}",
                workspaceId, targetUserId, currentUserId);
        }

        // PIPELINE: Visibility -> Permission (Members.Suspend) -> BR (Owner cannot be suspended; target must be Active).
        public async Task SuspendWorkspaceMemberAsync(long workspaceId, string targetUserId, string currentUserId, CancellationToken cancellationToken = default)
        {
            if (string.Equals(targetUserId, currentUserId, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("You cannot suspend yourself.");

            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.MembersSuspend,
                null);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("SuspendWorkspaceMember pipeline failed. Reason: {Reason}, WorkspaceId: {WorkspaceId}, CurrentUserId: {CurrentUserId}",
                    authResult.FailureReason, workspaceId, currentUserId);
                throw authResult.ToAuthorizationException();
            }

            var target = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.WorkspaceId == workspaceId && wm.UserId == targetUserId && !wm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (target is null)
                throw new NotFoundException("This user is not a member of this workspace.");

            if (target.Status == WorkspaceMemberStatus.Removed)
                throw new BadRequestException("This user has already been removed from the workspace.");

            if (target.Role == WorkspaceRole.Owner)
                throw new ForbiddenException("The workspace Owner cannot be suspended.");

            if (target.Status == WorkspaceMemberStatus.Suspended)
                throw new BadRequestException("This member is already suspended.");

            var oldValues = JsonSerializer.Serialize(new { UserId = targetUserId, Status = target.Status });

            target.Status = WorkspaceMemberStatus.Suspended;
            target.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.WorkspaceMembers.Update(target);

            await _auditLogService.LogAsync(currentUserId, "Suspend Workspace Member", nameof(WorkspaceMember), target.Id.ToString(), workspaceId: workspaceId, oldValues: oldValues, newValues: null, cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Workspace member suspended. WorkspaceId: {WorkspaceId}, TargetUserId: {TargetUserId}, By: {CurrentUserId}",
                workspaceId, targetUserId, currentUserId);
        }

        // PIPELINE: Visibility -> Permission (Members.Suspend) -> BR (target must be Suspended).
        public async Task UnsuspendWorkspaceMemberAsync(long workspaceId, string targetUserId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.MembersSuspend,
                null);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("UnsuspendWorkspaceMember pipeline failed. Reason: {Reason}, WorkspaceId: {WorkspaceId}, CurrentUserId: {CurrentUserId}",
                    authResult.FailureReason, workspaceId, currentUserId);
                throw authResult.ToAuthorizationException();
            }

            var target = await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .Where(wm => wm.WorkspaceId == workspaceId && wm.UserId == targetUserId && !wm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (target is null)
                throw new NotFoundException("This user is not a member of this workspace.");

            if (target.Status != WorkspaceMemberStatus.Suspended)
                throw new BadRequestException("This member is not suspended.");

            var oldValues = JsonSerializer.Serialize(new { UserId = targetUserId, Status = target.Status });

            target.Status = WorkspaceMemberStatus.Active;
            target.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.WorkspaceMembers.Update(target);

            await _auditLogService.LogAsync(currentUserId, "Unsuspend Workspace Member", nameof(WorkspaceMember), target.Id.ToString(), workspaceId: workspaceId, oldValues: oldValues, newValues: null, cancellationToken: cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Workspace member unsuspended. WorkspaceId: {WorkspaceId}, TargetUserId: {TargetUserId}, By: {CurrentUserId}",
                workspaceId, targetUserId, currentUserId);
        }

        // PIPELINE: Visibility -> Permission (WorkspaceView) only.
        public async Task<IEnumerable<WorkspaceMember>> GetWorkspaceMembersAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default)
        {
            var authResult = await _authService.AuthorizeAsync(
                workspaceId,
                currentUserId,
                Permissions.WorkspaceView,
                null);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("GetWorkspaceMembers pipeline failed. Reason: {Reason}, UserId: {CurrentUserId}, WorkspaceId: {WorkspaceId}", authResult.FailureReason, currentUserId, workspaceId);
                throw authResult.ToAuthorizationException();
            }

            return await _unitOfWork.WorkspaceMembers.GetAllQuery()
                .AsNoTracking()
                .Where(wm => wm.WorkspaceId == workspaceId && wm.Status != WorkspaceMemberStatus.Removed)
                .Include(wm => wm.User)
                .ToListAsync(cancellationToken);
        }
    }
}
