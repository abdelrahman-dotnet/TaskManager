using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Repositories;

public class Repository<T> : IGenericRepository<T>
    where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetByIdAsync<TKey>(TKey id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(id,cancellationToken);
    }

    public virtual IQueryable<T> GetAllQuery()
    {
        return _dbSet; // already iqueryable
    }

    public virtual async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate
        ,CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(predicate,cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate == null)
            return await _dbSet.CountAsync(cancellationToken);

        return await _dbSet.CountAsync(predicate,cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity,cancellationToken);
    }

    public virtual void Update(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
    }

    // FIX: previously this called _dbSet.Remove(entity) unconditionally,
    // which is a hard delete — completely inconsistent with BaseEntity's
    // IsDeleted flag and the global query filter configured in AppDbContext.
    // Now: entities that inherit BaseEntity are soft-deleted (flag set,
    // filtered out by the global query filter going forward). Anything else
    // (e.g. RolePermission, a pure join entity with no BaseEntity) still gets
    // physically removed since there's no IsDeleted flag to set.
    public virtual void Delete(T entity)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsDeleted = true;
            baseEntity.UpdatedAt = DateTime.UtcNow;
            baseEntity.DeletedAt = DateTime.UtcNow;
            _context.Entry(entity).State = EntityState.Modified;
        }
        else
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual void HardDelete(T entity)
    {
        _dbSet.Remove(entity);
    }
}
