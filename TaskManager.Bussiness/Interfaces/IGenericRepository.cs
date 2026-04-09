using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager.Bussiness.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllWithIncludeAsync(params Expression<Func<T, object>>[] Includes);
        IQueryable<T> GetAllAsQuerable();
        Task<T?> GetByIdAsync(object id);
        Task<T?> GetByIdWithIncludesAsync(object id,params Expression<Func<T, object>>[] includes);
        Task AddAsync(T entity);
        void Update(T? entity);
        void Delete(T? entity);
        //Task Complete();

    }
}
