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
    private IGenericRepository<TaskItemStatusHistory>? _TaskItemStatusHistories;
    private IGenericRepository<Permission>? _permissions;
    private IGenericRepository<RolePermission>? _rolePermissions;

    private IWorkspaceRepository? _workspaces;
    private IGenericRepository<WorkspaceMember>? _workspaceMembers;
    private IGenericRepository<TeamMember>? _teamMembers;
    private IGenericRepository<ProjectMember>? _projectMembers;
    private IGenericRepository<ProjectTeam>? _projectTeams;
    private IGenericRepository<Invitation>? _invitations;
    private IGenericRepository<CommentMention>? _commentMentions;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public ITaskRepository Tasks => _tasks ??= new TaskRepository(_context);
    public IProjectRepository Projects => _projects ??= new ProjectRepository(_context);
    public IGenericRepository<Team> Teams => _teams ??= new Repository<Team>(_context);
    public ICommentRepository Comments => _comments ??= new CommentRepository(_context);
    public IAttachmentRepository Attachments => _attachments ??= new AttachmentRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);
    public IGenericRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);
    public IGenericRepository<TaskAssignment> TaskAssignments => _taskAssignments ??= new Repository<TaskAssignment>(_context);
    public IGenericRepository<TaskItemStatusHistory> TaskItemStatusHistories => _TaskItemStatusHistories ??= new Repository<TaskItemStatusHistory>(_context);
    public IGenericRepository<Permission> Permissions => _permissions ??= new Repository<Permission>(_context);
    public IGenericRepository<RolePermission> RolePermissions => _rolePermissions ??= new Repository<RolePermission>(_context);

    public IWorkspaceRepository Workspaces => _workspaces ??= new WorkspaceRepository(_context);
    public IGenericRepository<WorkspaceMember> WorkspaceMembers => _workspaceMembers ??= new Repository<WorkspaceMember>(_context);
    public IGenericRepository<TeamMember> TeamMembers => _teamMembers ??= new Repository<TeamMember>(_context);
    public IGenericRepository<ProjectMember> ProjectMembers => _projectMembers ??= new Repository<ProjectMember>(_context);
    public IGenericRepository<ProjectTeam> ProjectTeams => _projectTeams ??= new Repository<ProjectTeam>(_context);
    public IGenericRepository<Invitation> Invitations => _invitations ??= new Repository<Invitation>(_context);
    public IGenericRepository<CommentMention> CommentMentions => _commentMentions ??= new Repository<CommentMention>(_context);

    public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
