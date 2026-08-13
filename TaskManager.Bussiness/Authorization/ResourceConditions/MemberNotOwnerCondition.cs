using TaskManager.Data.Entities;
using TaskManager.Data.Enums;

namespace TaskManager.Bussiness.Authorization.ResourceConditions
{
    /// <summary>
    /// قيد Members.Remove / Members.ChangeRole — يمنع الإجراء على عضو دوره Owner
    /// (لازم TransferOwnership الأول). بيتطبق على كل الـ Roles بلا استثناء.
    /// </summary>
    public class MemberNotOwnerCondition : IResourceCondition
    {
        private readonly WorkspaceMember _targetMember;

        public MemberNotOwnerCondition(WorkspaceMember targetMember) => _targetMember = targetMember;

        public string FailureMessage =>
            "Cannot remove or change the role of the workspace Owner. Transfer ownership first.";

        public Task<bool> IsSatisfiedAsync(WorkspaceMember currentMember)
        {
            return Task.FromResult(_targetMember.Role != WorkspaceRole.Owner);
        }
    }
}
