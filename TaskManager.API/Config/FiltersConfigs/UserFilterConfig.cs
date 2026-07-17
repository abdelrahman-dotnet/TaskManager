using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class UserFilterConfig
    {
        public static readonly Dictionary<UserFilterField, Func<object, Expression<Func<ApplicationUser, bool>>>> map
            = new()
            {
                [UserFilterField.UserName] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.UserName.Contains(val);
                },
                [UserFilterField.IsActive] = value =>
                {
                    var val = (bool)value;
                    return x => x.IsActive == val;
                },
                [UserFilterField.TeamId] = value =>
                {
                    var val = (long)value;
                    return x => x.TeamId == val;
                },
                [UserFilterField.LastLoginAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.LastLoginAt != null && x.LastLoginAt.Value >= val;
                },
                [UserFilterField.CreatedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.CreatedAt >= val;
                }
            };
    }
}