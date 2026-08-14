using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Authorization.ResourceConditions
{
    /// <summary>
    /// Stage-3 Resource Condition (D-06a / D-17): an archived task is read-only.
    /// Any write operation against an archived task is rejected with 403 regardless
    /// of role — Owner/Admin do not bypass this condition (spec-level invariant, not a
    /// permission). Use TaskDeleteCondition/TaskArchivedCondition combinations as
    /// required per operation.
    /// </summary>
    public class TaskArchivedCondition : IResourceCondition
    {
        private readonly TaskItem _task;

        public TaskArchivedCondition(TaskItem task) => _task = task;

        public string FailureMessage =>
            "This task is archived. Restore it first before performing this operation.";

        public Task<bool> IsSatisfiedAsync(WorkspaceMember currentMember)
        {
            return Task.FromResult(!_task.IsArchived);
        }
    }
}
