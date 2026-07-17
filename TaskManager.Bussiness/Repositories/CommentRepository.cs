using Microsoft.EntityFrameworkCore;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;
using TaskManager.Data.Repositories;

namespace TaskManager.Bussiness.Repositories
{
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        public CommentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Comment>> GetByTaskIdAsync(long taskId)
        {
            return await _context.Comments
                .Where(c => c.TaskItemId == taskId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}