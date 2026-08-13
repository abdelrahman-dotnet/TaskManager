using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Interfaces
{
    public interface IWorkspaceRepository : IGenericRepository<Workspace>
    {
        Task<Workspace?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
        Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
        Task<Workspace?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);
    }
}
