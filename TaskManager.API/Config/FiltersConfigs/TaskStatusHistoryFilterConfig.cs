using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class TaskStatusHistoryFilterConfig
    {
        public static readonly Dictionary<TaskStatusHistoryFilterFields, Func<object, Expression<Func<TaskStatusHistory, bool>>>> map
            = new()
            {
                [TaskStatusHistoryFilterFields.TaskItemId] = value =>
                {
                    var val = (long)value;
                    return x => x.TaskItemId == val;
                },
                [TaskStatusHistoryFilterFields.OldStatus] = value =>
                {
                    var val = (TaskItemStatus)value;
                    return x => x.OldStatus == val;
                },
                [TaskStatusHistoryFilterFields.NewStatus] = value =>
                {
                    var val = (TaskItemStatus)value;
                    return x => x.NewStatus == val;
                },
                [TaskStatusHistoryFilterFields.ChangedByUserId] = value =>
                {
                    var val = value.ToString();
                    return x => x.ChangedByUserId == val;
                },
                [TaskStatusHistoryFilterFields.ChangedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.ChangedAt.Date == val.Date;
                }
            };
    }
}
