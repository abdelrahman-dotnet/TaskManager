using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Interfaces
{
    public interface IProjectRepository : IGenericRepository<Project>
    {
        Task<Project?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);
    }
}
