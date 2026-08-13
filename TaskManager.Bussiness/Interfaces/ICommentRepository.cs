using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Interfaces
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetByTaskIdAsync(long taskId, CancellationToken cancellationToken = default);
    }
}
