using Microsoft.EntityFrameworkCore;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;
using TaskManager.Data.Repositories;

namespace TaskManager.Bussiness.Repositories
{
    public class WorkspaceRepository : Repository<Workspace>, IWorkspaceRepository
    {
        public WorkspaceRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Workspace?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(w => w.Slug == slug, cancellationToken);
        }

        public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(w => w.Slug == slug, cancellationToken);
        }

        public async Task<Workspace?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(w => w.Members)
                    .ThenInclude(m => m.User)
                .Include(w => w.Teams)
                .Include(w => w.Projects)
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        }
    }
}
