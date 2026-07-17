using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class TaskAssignmentFilterConfig
    {
        public static readonly Dictionary<TaskAssignmentFilterFields, Func<object, Expression<Func<TaskAssignment, bool>>>> map
            = new()
            {
                [TaskAssignmentFilterFields.TaskItemId] = value =>
                {
                    var val = (long)value;
                    return x => x.TaskItemId == val;
                },
                [TaskAssignmentFilterFields.UserId] = value =>
                {
                    var val = value.ToString();
                    return x => x.UserId == val;
                },
                [TaskAssignmentFilterFields.AssignedByUserId] = value =>
                {
                    var val = value.ToString();
                    return x => x.AssignedByUserId == val;
                },
                [TaskAssignmentFilterFields.AssignedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.AssignedAt.Date == val.Date;
                }
            };
    }
}
