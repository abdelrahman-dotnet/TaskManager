using System.Linq.Expressions;
using TaskManager.API.Enums.FilterFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config.FiltersConfigs
{
    public static class AttachmentFilterConfig
    {
        public static readonly Dictionary<AttachmentFilterFields, Func<object, Expression<Func<Attachment, bool>>>> map
            = new()
            {
                [AttachmentFilterFields.FileName] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.FileName.Contains(val);
                },
                [AttachmentFilterFields.ContentType] = value =>
                {
                    var val = value.ToString()!;
                    return x => x.ContentType == val;
                },
                [AttachmentFilterFields.TaskItemId] = value =>
                {
                    var val = (long)value;
                    return x => x.TaskItemId == val;
                },
                [AttachmentFilterFields.UploadedByUserId] = value =>
                {
                    var val = value.ToString();
                    return x => x.UploadedByUserId == val;
                },
                [AttachmentFilterFields.CreatedAt] = value =>
                {
                    var val = (DateTime)value;
                    return x => x.CreatedAt.Date == val.Date;
                }
            };
    }
}
