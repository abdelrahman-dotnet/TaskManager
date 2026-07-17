using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class ProjectFilterConfig
    {
        public static readonly Dictionary<ProjectFilterFields, Func<object, Expression<Func<Project, bool>>>> map
            = new()
            {
                [ProjectFilterFields.Name] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Name.Contains(val);
                },
                [ProjectFilterFields.Description] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Description != null && x.Description.Contains(val);
                },
                [ProjectFilterFields.IsArchived] = value =>
                {
                    var val = (bool)value;
                    return x => x.IsArchived == val;
                },
                [ProjectFilterFields.TeamId] = value =>
                {
                    var val = (long)value;
                    return x => x.TeamId == val;
                },
                [ProjectFilterFields.CreatedByUserId] = value =>
                {
                    var val = value.ToString();
                    return x => x.CreatedByUserId == val;
                },
                [ProjectFilterFields.StartDate] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.StartDate != null && x.StartDate.Value.Date == val.Date;
                },
                [ProjectFilterFields.EndDate] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.EndDate != null && x.EndDate.Value.Date == val.Date;
                },
                [ProjectFilterFields.CreatedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.CreatedAt.Date == val.Date;
                }
            };
    }
}
