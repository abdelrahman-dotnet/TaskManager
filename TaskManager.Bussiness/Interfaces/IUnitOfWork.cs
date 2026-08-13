using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Entities;

namespace TaskManager.Business.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    ITaskRepository Tasks { get; }
    IProjectRepository Projects { get; }
    IGenericRepository<Team> Teams { get; }
    ICommentRepository Comments { get; }
    IAttachmentRepository Attachments { get; }
    INotificationRepository Notifications { get; }
    IGenericRepository<AuditLog> AuditLogs { get; }
    IGenericRepository<TaskAssignment> TaskAssignments { get; }
    IGenericRepository<TaskItemStatusHistory> TaskItemStatusHistories { get; }
    IGenericRepository<Permission> Permissions { get; }
    IGenericRepository<RolePermission> RolePermissions { get; }

    // NEW (Multi-tenant & Membership System)
    IWorkspaceRepository Workspaces { get; }
    IGenericRepository<WorkspaceMember> WorkspaceMembers { get; }
    IGenericRepository<TeamMember> TeamMembers { get; }
    IGenericRepository<ProjectMember> ProjectMembers { get; }
    IGenericRepository<ProjectTeam> ProjectTeams { get; }
    IGenericRepository<Invitation> Invitations { get; }

    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}
