using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class CommentFilterConfig
    {
        public static readonly Dictionary<CommentFilterField, Func<object, Expression<Func<Comment, bool>>>> map
            = new()
            {
                [CommentFilterField.Content] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Content.Contains(val);
                },
                [CommentFilterField.DateFrom] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.CreatedAt >= val;
                },
                [CommentFilterField.DateTo] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.CreatedAt <= val;
                },
                [CommentFilterField.TaskId] = value =>
                {
                    var val = (long)value;
                    return x => x.TaskItemId == val;
                },
                [CommentFilterField.UserId] = value =>
                {
                    var val = value.ToString();
                    return x => x.UserId == val;
                }
            };
    }
}
