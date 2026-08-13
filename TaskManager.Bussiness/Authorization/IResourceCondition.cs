using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Authorization
{
    /// <summary>
    /// شرط خاص بحالة الـ Resource نفسه (مرحلة 3 من الـ Pipeline).
    /// كل Feature محتاجة شرط خاص بتعمل Implementation منفصل.
    /// </summary>
    public interface IResourceCondition
    {
        /// <returns>true لو الشرط متحقق (مسموح يكمل)، false لو لأ (403)</returns>
        Task<bool> IsSatisfiedAsync(WorkspaceMember currentMember);

        /// <summary>الرسالة اللي هتترجع في الـ 403 لو الشرط فشل — خليها تفصيلية عشان الـ Debugging.</summary>
        string FailureMessage { get; }
    }
}
