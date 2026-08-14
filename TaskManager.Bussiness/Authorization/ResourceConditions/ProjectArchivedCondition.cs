using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Authorization.ResourceConditions
{
    /// <summary>
    /// Stage-3 Resource Condition (D-06a / D-17): an archived project is read-only.
    /// Any write operation against an archived project is rejected with 403 regardless
    /// of role — Owner/Admin do not bypass this condition (spec-level invariant, not a
    /// permission). Project Archive/Restore operations obviously pass their own
    /// (pre-archived / archived) instance instead.
    /// </summary>
    public class ProjectArchivedCondition : IResourceCondition
    {
        private readonly Project _project;
        private readonly bool _allowArchived;

        /// <param name="allowArchived">true when the operation itself targets an archived project (Restore / detach).</param>
        public ProjectArchivedCondition(Project project, bool allowArchived = false)
        {
            _project = project;
            _allowArchived = allowArchived;
        }

        public string FailureMessage =>
            "This project is archived. Restore it first before performing this operation.";

        public Task<bool> IsSatisfiedAsync(WorkspaceMember currentMember)
        {
            return Task.FromResult(_allowArchived || !_project.IsArchived);
        }
    }
}
