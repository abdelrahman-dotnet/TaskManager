using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Authorization
{
    public interface IWorkspaceAuthorizationService
    {
        /// <summary>
        /// بينفذ المراحل 1+2+3 من الـ Pipeline (Visibility → Permission → Resource Condition).
        /// المراحل 4 و5 (Business Rules + Operation) مسؤولية الـ Service اللي بينده الميثود دي.
        /// </summary>
        /// <param name="workspaceId">الـ Workspace اللي العملية بتحصل فيه</param>
        /// <param name="currentUserId">الـ UserId بتاع الشخص اللي بيحاول ينفذ العملية</param>
        /// <param name="permission">من TaskManager.Bussiness.Authorization.Permissions، مثلاً Permissions.Tasks.Delete</param>
        /// <param name="resourceCondition">اختياري — null لو الـ Permission مفيهاش شرط إضافي على الـ Resource</param>
        Task<AuthorizationResult> AuthorizeAsync(
            long workspaceId,
            string currentUserId,
            string permission,
            IResourceCondition? resourceCondition = null);

        /// <summary>
        /// Evaluates the existing visibility, permission, and resource-condition stages
        /// for all of a caller's workspace memberships using one set-based eligibility read.
        /// Intended for recipient-scoped list composition where individual failures are
        /// filtered out rather than surfaced as a single endpoint-level result.
        /// </summary>
        Task<List<long>> GetAuthorizedWorkspaceMemberIdsAsync(
            string currentUserId,
            string permission,
            Func<WorkspaceMember, IResourceCondition?> resourceConditionFactory,
            CancellationToken cancellationToken = default);
    }
}
