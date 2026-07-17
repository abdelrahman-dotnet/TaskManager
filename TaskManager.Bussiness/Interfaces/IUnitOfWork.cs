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

    Task<int> CompleteAsync(CancellationToken cancellationToken = default);

    //Task BeginTransactionAsync();

    //Task CommitTransactionAsync();

    //Task RollbackTransactionAsync();
}