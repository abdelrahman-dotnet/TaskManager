using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class TaskFilterConfig
    {
        public static readonly Dictionary<TaskFilterFields, Func<object, Expression<Func<TaskItem, bool>>>> map
            = new()
            {
                [TaskFilterFields.Title] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Title.Contains(val);
                },
                [TaskFilterFields.Description] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Description != null && x.Description.Contains(val);
                },
                [TaskFilterFields.Status] = value =>
                {
                    var val = (TaskItemStatus)value;
                    return x => x.Status == val;
                },
                [TaskFilterFields.Priority] = value =>
                {
                    var val = (TaskPriority)value;
                    return x => x.Priority == val;
                },
                [TaskFilterFields.ProjectId] = value =>
                {
                    var val = (long)value;
                    return x => x.ProjectId == val;
                },
                [TaskFilterFields.CreatedByUserId] = value =>
                {
                    var val = value.ToString();
                    return x => x.CreatedByUserId == val;
                },
                [TaskFilterFields.DueDate] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.DueDate != null && x.DueDate.Value.Date == val.Date;
                },
                [TaskFilterFields.CompletedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.CompletedAt != null && x.CompletedAt.Value.Date == val.Date;
                },
                [TaskFilterFields.CreatedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.CreatedAt.Date == val.Date;
                }
            };
    }
}
