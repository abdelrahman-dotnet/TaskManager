using TaskManager.Data.Entities;
using TaskManager.Data.Enums;

namespace TaskManager.Bussiness.Authorization.ResourceConditions
{
    /// <summary>
    /// قيد Members.ChangeRole / Members.Suspend / Members.Remove عند تنفيذها من Admin:
    /// الفحص: TargetMember.Role == Member (يحمي الـ Owner والـ Admins الآخرين من بعضهم).
    /// (S-12 من وثيقة المصفوفة المعتمدة — "لا يستطيع Administrator تغيير دور،
    /// أو تعليق، أو إزالة Administrator آخر أو Owner").
    /// ملاحظة: الـ Owner مش مسموح له أصلاً من الماتركس إنه يستهدف Owner
    /// (TransferOwnership فقط)، والـ Admin هو الوحيد اللي عنده الصلاحية المقيدة.
    /// </summary>
    public class MemberProtectedRoleCondition : IResourceCondition
    {
        private readonly WorkspaceMember _targetMember;

        public MemberProtectedRoleCondition(WorkspaceMember targetMember) => _targetMember = targetMember;

        public string FailureMessage =>
            "You can only change role, suspend, or remove members with the 'Member' role. " +
            "Owners and Administrators are protected.";

        public Task<bool> IsSatisfiedAsync(WorkspaceMember currentMember)
        {
            // الحماية بتتنفذ على مستوى الـ Target نفسه — مش هيتأثر أي دور بيكون Admin:
            return Task.FromResult(_targetMember.Role == WorkspaceRole.Member);
        }
    }
}
