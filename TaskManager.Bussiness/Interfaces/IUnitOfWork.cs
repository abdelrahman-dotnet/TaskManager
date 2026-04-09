using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager.Bussiness.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository UserRepository { get; }
        IRoleRepository roleRepository { get; }
        ITaskRepository taskRepository { get; } 
        ICommentRepository commentRepository { get; }
        Task<int> Complete();
    }
}
