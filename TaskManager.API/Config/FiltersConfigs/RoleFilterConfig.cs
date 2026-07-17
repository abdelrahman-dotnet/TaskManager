using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class RoleFilterConfig
    {
        public static readonly Dictionary<RoleFilterField, Func<object, Expression<Func<ApplicationRole, bool>>>> map
            = new()
            {
                [RoleFilterField.Name] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Name!.Contains(val);
                },
                [RoleFilterField.Description] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Description != null && x.Description.Contains(val);
                }
            };
    }
}
