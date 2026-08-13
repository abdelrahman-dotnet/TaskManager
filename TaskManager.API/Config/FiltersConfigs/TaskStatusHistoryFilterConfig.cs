using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class TaskItemStatusHistoryFilterConfig
    {
        public static readonly Dictionary<TaskItemStatusHistoryFilterFields, Func<object, Expression<Func<TaskItemStatusHistory, bool>>>> map
            = new()
            {
                [TaskItemStatusHistoryFilterFields.TaskItemId] = value =>
                {
                    var val = (long)value;
                    return x => x.TaskItemId == val;
                },
                [TaskItemStatusHistoryFilterFields.OldStatus] = value =>
                {
                    var val = (TaskItemStatus)value;
                    return x => x.OldStatus == val;
                },
                [TaskItemStatusHistoryFilterFields.NewStatus] = value =>
                {
                    var val = (TaskItemStatus)value;
                    return x => x.NewStatus == val;
                },
                [TaskItemStatusHistoryFilterFields.ChangedByUserId] = value =>
                {
                    var val = Convert.ToInt64(value);
                    return x => x.ChangedByWorkspaceMemberId == val;
                },
                [TaskItemStatusHistoryFilterFields.ChangedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.ChangedAt.Date == val.Date;
                }
            };
    }
}
