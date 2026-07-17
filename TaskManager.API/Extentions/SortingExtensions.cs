using System.Linq.Expressions;
using TaskManager.API.DTOs.Params;
using TaskManager.API.Enums;

namespace TaskManager.API.Extentions
{
    public static class SortingExtensions
    {
        public static IQueryable<T>
            ApplySorting<T, TSortEnum>(
            this IQueryable<T> query,

            List<SortOption<TSortEnum>> sorts,

            Dictionary<TSortEnum,Expression<Func<T, object>>> map,

            // Guarantees a deterministic order even when the client sends no
            // sort at all, or when the chosen field has ties. Without this,
            // Skip/Take on an unordered query gives unpredictable results
            // (rows can repeat or go missing across pages).
            Expression<Func<T, object>>? tieBreaker = null) where TSortEnum : Enum
            {
            IOrderedQueryable<T>? ordered = null;

            if (sorts != null)
            {
                foreach (var sort in sorts)
                {
                    if (!map.TryGetValue(
                        sort.Field,
                        out var expr))
                    {
                        continue;
                    }

                    if (ordered == null)
                    {
                        ordered = sort.Direction == SortDirection.Descending

                            ? query.OrderByDescending(expr) : query.OrderBy(expr);
                    }
                    else
                    {
                        ordered = sort.Direction == SortDirection.Descending ? ordered.ThenByDescending(expr) : ordered.ThenBy(expr);
                    }
                }
            }

            if (tieBreaker == null)
                return (IQueryable<T>?)ordered ?? query;

            return ordered == null
                ? query.OrderBy(tieBreaker)
                : ordered.ThenBy(tieBreaker);
        }
    }
}