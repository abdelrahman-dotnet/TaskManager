using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class NotificationFilterConfig
    {
        public static readonly Dictionary<NotificationFilterFields, Func<object, Expression<Func<Notification, bool>>>> map
            = new()
            {
                [NotificationFilterFields.Title] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Title.Contains(val);
                },
                [NotificationFilterFields.IsRead] = value =>
                {
                    var val = (bool)value;
                    return x => x.IsRead == val;
                },
                [NotificationFilterFields.UserId] = value =>
                {
                    var val = value.ToString();
                    return x => x.UserId == val;
                },
                [NotificationFilterFields.CreatedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.CreatedAt.Date == val.Date;
                }
            };
    }
}
