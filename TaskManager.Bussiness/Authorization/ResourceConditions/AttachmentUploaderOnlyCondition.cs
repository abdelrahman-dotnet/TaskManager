using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Authorization.ResourceConditions
{
    /// <summary>
    /// قيد Attachments.Delete للـ Member — Uploader-only.
    /// الفحص: CurrentMember.Id == Attachment.UploadedByWorkspaceMemberId
    /// (S-14 من وثيقة المصفوفة المعتمدة).
    /// بيتطبق على كل الـ Roles بلا استثناء — حتى Owner/Admin ميقدرش يحذف مرفق حد تاني
    /// عبر الـ Pipeline.
    /// </summary>
    public class AttachmentUploaderOnlyCondition : IResourceCondition
    {
        private readonly Attachment _attachment;

        public AttachmentUploaderOnlyCondition(Attachment attachment) => _attachment = attachment;

        public string FailureMessage => "Only the member who uploaded this attachment can delete it.";

        public Task<bool> IsSatisfiedAsync(WorkspaceMember currentMember)
        {
            return Task.FromResult(_attachment.UploadedByWorkspaceMemberId == currentMember.Id);
        }
    }
}
