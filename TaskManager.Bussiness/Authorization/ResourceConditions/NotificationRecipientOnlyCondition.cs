using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Authorization.ResourceConditions
{
    /// <summary>
    /// قيد Notification.UpdateState (Mark Read/Unread) — Recipient-only.
    /// الفحص: CurrentMember.Id == Notification.WorkspaceMemberId
    /// (S-15 من وثيقة المصفوفة المعتمدة).
    /// بيتطبق على كل الـ Roles بلا استثناء — حتى Owner/Admin ميقدرش يعلّم إشعار حد تاني
    /// كمقروء/غير مقروء.
    /// </summary>
    public class NotificationRecipientOnlyCondition : IResourceCondition
    {
        private readonly Notification _notification;

        public NotificationRecipientOnlyCondition(Notification notification) => _notification = notification;

        public string FailureMessage => "You can only update the read state of your own notifications.";

        public Task<bool> IsSatisfiedAsync(WorkspaceMember currentMember)
        {
            return Task.FromResult(_notification.WorkspaceMemberId == currentMember.Id);
        }
    }
}
