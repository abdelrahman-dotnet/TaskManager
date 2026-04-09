using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Context;

namespace TaskManager.Bussiness.Repositories
{
    public class UnitOfWorkRepo : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;

        public UnitOfWorkRepo(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            UserRepository = new UserRepository(_dbContext);
            roleRepository = new RoleRepository(_dbContext);
            taskRepository = new TaskRepository(_dbContext);
            commentRepository = new CommentRepository(_dbContext);
        }
        public IUserRepository UserRepository {  get; set; }

        public IRoleRepository roleRepository {  get; set; }

        public ITaskRepository taskRepository {  get; set; }

        public ICommentRepository commentRepository { get; set; }

        public async Task<int> Complete()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
