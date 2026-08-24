using Microsoft.EntityFrameworkCore;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;
using TaskManager.Data.Repositories;

namespace TaskManager.Bussiness.Repositories
{
    public class TaskRepository : Repository<TaskItem>, ITaskRepository
    {
        public TaskRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<TaskItem?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Project)
                .Include(t => t.CreatedByUser)
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.WorkspaceMember)
                        .ThenInclude(wm => wm.User)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.WorkspaceMember)
                        .ThenInclude(wm => wm.User)
                .Include(t => t.Attachments)
                .Include(t => t.StatusHistory)
                    .ThenInclude(h => h.ChangedByWorkspaceMember)
                        .ThenInclude(wm => wm.User)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
    }
}
