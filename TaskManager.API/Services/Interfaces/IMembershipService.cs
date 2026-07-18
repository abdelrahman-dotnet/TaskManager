using TaskManager.Data.Entities;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IMembershipService
    {
        // ── Access Checks ────────────────────────────────────────────────────────
        // IsXMemberAsync = the raw membership fact (a row exists in TeamMember/ProjectMember).
        // CanAccessXAsync = the Business Rule ("is this user allowed to see/act on X at all").
        // They're 1:1 today (CanAccessTeamAsync just calls IsTeamMemberAsync), but kept as
        // separate methods on purpose: the day Membership grows a "Suspended" or "Invitation
        // Pending" state, CanAccess changes and IsMember doesn't - callers that asked the
        // Business Rule question don't need to change.
        Task<bool> IsTeamMemberAsync(long teamId, string userId, CancellationToken cancellationToken = default);
        Task<bool> IsProjectMemberAsync(long projectId, string userId, CancellationToken cancellationToken = default);

        Task<bool> CanAccessTeamAsync(long teamId, string userId, CancellationToken cancellationToken = default);
        Task<bool> CanAccessProjectAsync(long projectId, string userId, CancellationToken cancellationToken = default);
        // Composite: Task -> Project -> ProjectMember.
        Task<bool> CanAccessTaskAsync(long taskId, string userId, CancellationToken cancellationToken = default);

        // ── Role Checks ──────────────────────────────────────────────────────────
        Task<MembershipRole?> GetUserTeamRoleAsync(long teamId, string userId, CancellationToken cancellationToken = default);
        Task<MembershipRole?> GetUserProjectRoleAsync(long projectId, string userId, CancellationToken cancellationToken = default);

        Task<bool> IsTeamOwnerAsync(long teamId, string userId, CancellationToken cancellationToken = default);
        Task<bool> IsProjectOwnerAsync(long projectId, string userId, CancellationToken cancellationToken = default);

        // Throws ForbiddenException if the user isn't Owner/Manager of the Team/Project -
        // callers don't repeat "if (!await CanManageX(...)) throw new ForbiddenException(...)"
        // in every Service; they just await this and keep going.
        Task EnsureCanManageTeamAsync(long teamId, string userId, CancellationToken cancellationToken = default);
        Task EnsureCanManageProjectAsync(long projectId, string userId, CancellationToken cancellationToken = default);

        // ── Listings ─────────────────────────────────────────────────────────────
        Task<IEnumerable<TeamMember>> GetTeamMembersAsync(long teamId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(long projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<TeamMember>> GetUserTeamMembershipsAsync(string userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProjectMember>> GetUserProjectMembershipsAsync(string userId, CancellationToken cancellationToken = default);

        // ── Mutations (backing the future Management APIs) ──────────────────────
        Task AddTeamMemberAsync(long teamId, string userId, MembershipRole role, string currentUserId, CancellationToken cancellationToken = default);
        Task RemoveTeamMemberAsync(long teamId, string userId, string currentUserId, CancellationToken cancellationToken = default);
        Task ChangeTeamMemberRoleAsync(long teamId, string userId, MembershipRole newRole, string currentUserId, CancellationToken cancellationToken = default);

        Task AddProjectMemberAsync(long projectId, string userId, MembershipRole role, string currentUserId, CancellationToken cancellationToken = default);
        Task RemoveProjectMemberAsync(long projectId, string userId, string currentUserId, CancellationToken cancellationToken = default);
        Task ChangeProjectMemberRoleAsync(long projectId, string userId, MembershipRole newRole, string currentUserId, CancellationToken cancellationToken = default);
    }
}
