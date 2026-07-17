using Microsoft.EntityFrameworkCore;
using System.Threading;
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

        public async Task<TaskItem?> GetDetailsAsync(long id,CancellationToken cancellationToken = default)
        {
            return await _context.TaskItems
                .AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.CreatedByUser)
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.User)
                .Include(t => t.Comments)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.Id == id,cancellationToken);
        }
    }
}
