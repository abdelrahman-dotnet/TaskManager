using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Repositories
{
    public class TaskRepository : GenericRepository<TaskItem> , ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<TaskItem?> GetTaskWithCommentsAsync (int taskId)
        {
            return await _context.taskItems
                .Include(t=>t.Comments)
                .Include(t=>t.User)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }
        public async Task<IEnumerable<TaskItem>> GetUserTaskAsync(string userId)
        {
            return await _context.taskItems
                .Where(t=>t.UserId == userId)
                .Include(t=>t.Comments)
                .OrderByDescending(t=>t.CreatedDate)
                .ToListAsync();
        }
    }
}
