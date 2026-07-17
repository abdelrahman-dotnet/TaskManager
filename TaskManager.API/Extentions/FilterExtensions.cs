using System.Linq.Expressions;

namespace TaskManager.API.Extentions
{
    public static class FilterExtensions
    {
        public static IQueryable<T> ApplyFiltering<T,TFilterEnum>(
            this IQueryable<T> query, object queryParams,
            Dictionary<TFilterEnum, Func<object, Expression<Func<T, bool>>>> map) where TFilterEnum : Enum
        {
            foreach (var item in map)
            {
                var enumKey = item.Key;
                var predicateFactory = item.Value;

                var propertyName = enumKey.ToString();
                // ComingFromUser 
                var prop = queryParams.GetType().GetProperty(propertyName);
                if (prop == null) 
                    continue;

                var value = prop.GetValue(queryParams);
                if (value == null ) 
                    continue;

                //var predicate = func(value);

                query = query.Where(predicateFactory(value));
            }
            return query;
        }
    }
}
