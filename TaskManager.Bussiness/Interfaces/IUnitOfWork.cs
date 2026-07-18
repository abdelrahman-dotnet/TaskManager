using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Entities;
using TaskManager.Data.Repositories;

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

    IGenericRepository<TaskStatusHistory> TaskStatusHistories { get; }

    IGenericRepository<Permission> Permissions { get; }

    IGenericRepository<RolePermission> RolePermissions { get; }

    // NEW (Membership System): plain IGenericRepository<T> - per the "repository is introduced
    // by repeated behavior, not by entity existence" decision, no specialized
    // ITeamMemberRepository/IProjectMemberRepository yet. Revisit only if IMembershipService
    // ends up needing a query complex/repeated enough to justify one.
    IGenericRepository<TeamMember> TeamMembers { get; }

    IGenericRepository<ProjectMember> ProjectMembers { get; }

    Task<int> CompleteAsync(CancellationToken cancellationToken = default);

    //Task BeginTransactionAsync();

    //Task CommitTransactionAsync();

    //Task RollbackTransactionAsync();
}
