using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class AuditLogFilterConfig
    {
        public static readonly Dictionary<AuditLogFilterFields, Func<object, Expression<Func<AuditLog, bool>>>> map
            = new()
            {
                [AuditLogFilterFields.Action] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.Action.Contains(val);
                },
                [AuditLogFilterFields.EntityName] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.EntityName == val;
                },
                [AuditLogFilterFields.EntityId] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.EntityId == val;
                },
                [AuditLogFilterFields.UserId] = value =>
                {
                    var val = value.ToString();
                    return x => x.UserId == val;
                },
                [AuditLogFilterFields.CreatedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.CreatedAt.Date == val.Date;
                }
            };
    }
}
