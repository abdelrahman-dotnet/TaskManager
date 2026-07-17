using Microsoft.EntityFrameworkCore.Storage;
using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Bussiness.Repositories;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;
using TaskManager.Data.Repositories;

namespace TaskManager.Data.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    private IDbContextTransaction? _transaction;

    private ITaskRepository? _tasks;
    private IProjectRepository? _projects;

    private IGenericRepository<Team>? _teams;
    private ICommentRepository? _comments;
    private IAttachmentRepository? _attachments;
    private INotificationRepository? _notifications;
    private IGenericRepository<AuditLog>? _auditLogs;
    private IGenericRepository<TaskAssignment>? _taskAssignments;
    private IGenericRepository<TaskStatusHistory>? _taskStatusHistories;
    private IGenericRepository<Permission>? _permissions;
    private IGenericRepository<RolePermission>? _rolePermissions;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public ITaskRepository Tasks
        => _tasks ??= new TaskRepository(_context);

    public IProjectRepository Projects
        => _projects ??= new ProjectRepository(_context);

    public IGenericRepository<Team> Teams
        => _teams ??= new Repository<Team>(_context);

    public ICommentRepository Comments
        => _comments ??= new CommentRepository(_context);

    public IAttachmentRepository Attachments
        => _attachments ??= new AttachmentRepository(_context);

    public INotificationRepository Notifications
        => _notifications ??= new NotificationRepository(_context);

    public IGenericRepository<AuditLog> AuditLogs
        => _auditLogs ??= new Repository<AuditLog>(_context);

    public IGenericRepository<TaskAssignment> TaskAssignments
        => _taskAssignments ??= new Repository<TaskAssignment>(_context);

    public IGenericRepository<TaskStatusHistory> TaskStatusHistories
        => _taskStatusHistories ??= new Repository<TaskStatusHistory>(_context);

    public IGenericRepository<Permission> Permissions
        => _permissions ??= new Repository<Permission>(_context);

    public IGenericRepository<RolePermission> RolePermissions
        => _rolePermissions ??= new Repository<RolePermission>(_context);

    public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync();
    }

    //public async Task BeginTransactionAsync()
    //{
    //    _transaction = await _context.Database.BeginTransactionAsync();
    //}

    //public async Task CommitTransactionAsync()
    //{
    //    if (_transaction is not null)
    //        await _transaction.CommitAsync();
    //}

    //public async Task RollbackTransactionAsync()
    //{
    //    if (_transaction is not null)
    //        await _transaction.RollbackAsync();
    //}

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
