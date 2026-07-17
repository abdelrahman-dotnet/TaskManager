using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Interfaces
{
    public interface ITaskRepository : IGenericRepository<TaskItem>
    {
        Task<TaskItem?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);
    }
}
