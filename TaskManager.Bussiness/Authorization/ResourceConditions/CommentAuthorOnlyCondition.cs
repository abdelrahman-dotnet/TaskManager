using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Authorization.ResourceConditions
{
    /// <summary>
    /// قيد Comments.Update/Delete — Author-only (S-13).
    /// بيتطبق على كل الـ Roles بلا استثناء — حتى Owner/Admin ميقدرش يعدّل كومنت حد تاني.
    /// </summary>
    public class CommentAuthorOnlyCondition : IResourceCondition
    {
        private readonly Comment _comment;

        public CommentAuthorOnlyCondition(Comment comment) => _comment = comment;

        public string FailureMessage => "Only the comment's author can update or delete it.";

        public Task<bool> IsSatisfiedAsync(WorkspaceMember currentMember)
        {
            return Task.FromResult(_comment.WorkspaceMemberId == currentMember.Id);
        }
    }
}
