using System.Linq.Expressions;

namespace TaskManager.Bussiness.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<T?> GetByIdAsync<TKey>(TKey id, CancellationToken cancellationToken = default);

        IQueryable<T> GetAllQuery();

        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes);

        Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        Task<int> CountAsync(
            Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        void Update(T entity, CancellationToken cancellationToken = default);

        // Soft-deletes when T inherits BaseEntity (sets IsDeleted = true),
        // otherwise falls back to a real removal.
        void Delete(T entity);

        // Explicit, physical row removal. Use only when you genuinely need to
        // purge data (GDPR erasure requests, cleanup jobs, etc.) — normal
        // "delete" flows in the app should call Delete() above.
        void HardDelete(T entity);
    }
}
