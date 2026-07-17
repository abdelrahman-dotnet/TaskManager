using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class TeamFilterConfig
    {
        public static readonly Dictionary<TeamFilterFields, Func<object, Expression<Func<Team, bool>>>> map
            = new()
            {
                [TeamFilterFields.Name] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Name.Contains(val);
                },
                [TeamFilterFields.Description] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Description != null && x.Description.Contains(val);
                },
                [TeamFilterFields.ManagerId] = value =>
                {
                    var val = value.ToString();
                    return x => x.ManagerId == val;
                },
                [TeamFilterFields.CreatedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.CreatedAt.Date == val.Date;
                }
            };
    }
}
