using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Context;

namespace TaskManager.Bussiness.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _dbContext;
        private readonly DbSet<T> _dbset;

        public GenericRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbset =  _dbContext.Set<T>();

        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbset.ToListAsync();
        }
        public async Task<IEnumerable<T>> GetAllWithIncludeAsync(params Expression<Func<T, object>>[] Includes)
        {
            IQueryable<T> query = _dbset;
            foreach (var include in Includes)
            {
                query = query.Include(include);
            }
            return await query.ToListAsync();
        }
        public IQueryable<T> GetAllAsQuerable()
        {
            return _dbset.AsQueryable();
        }
        public async Task<T?> GetByIdAsync(object id)
        {
            return await _dbset.FindAsync(id);
        }

        public async Task<T?> GetByIdWithIncludesAsync(object id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbset;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.FirstOrDefaultAsync(e => EF.Property<object>(e, "Id").Equals(id));
        }
        public async Task AddAsync(T entity)
        {
            await _dbset.AddAsync(entity);
        }
        public void Update(T? entity)
        {
             _dbset.Update(entity);
        }
        public void Delete(T? entity)
        {
            _dbset.Remove(entity);
        }

        //public async Task SaveChangesAsync()
        //{
        //    await _dbContext.SaveChangesAsync();
        //}

    }
}
