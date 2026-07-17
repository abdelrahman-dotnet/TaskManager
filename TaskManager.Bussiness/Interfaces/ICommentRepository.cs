using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Interfaces
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        // FIX: TaskItem.Id (and TaskItemId everywhere else) is a "long".
        // This parameter was an "int", which silently narrows/can't
        // represent ids above int.MaxValue. Aligned to "long".
        Task<IEnumerable<Comment>> GetByTaskIdAsync(long taskId);
    }
}
