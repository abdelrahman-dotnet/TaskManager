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
    }
}
