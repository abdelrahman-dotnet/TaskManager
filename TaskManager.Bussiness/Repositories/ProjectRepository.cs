using Microsoft.EntityFrameworkCore;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;
using TaskManager.Data.Repositories;

namespace TaskManager.Bussiness.Repositories
{
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<Project?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.ProjectTeams)
                    .ThenInclude(pt => pt.Team)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.WorkspaceMember)
                        .ThenInclude(wm => wm.User)
                .Include(p => p.Tasks)
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }
    }
}
