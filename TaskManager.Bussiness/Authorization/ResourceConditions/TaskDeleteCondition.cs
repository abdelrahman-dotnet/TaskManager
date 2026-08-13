using TaskManager.Data.Entities;
using TaskManager.Data.Enums;

namespace TaskManager.Bussiness.Authorization.ResourceConditions
{
    /// <summary>
    /// قيد Tasks.Delete على الـ Member:
    /// الشرطين لازم يتحققوا مع بعض: Task.Status == ToDo **و** Task.CreatedByMember == الحالي.
    /// Owner و Admin يتجاوزوا الشرط (لما الـ Permission بتاعهم يمر من المرحلة 2).
    /// </summary>
    public class TaskDeleteCondition : IResourceCondition
    {
        private readonly TaskItem _task;

        public TaskDeleteCondition(TaskItem task) => _task = task;

        public string FailureMessage =>
            "You can only delete a task that is still 'ToDo' and that you created.";

        public Task<bool> IsSatisfiedAsync(WorkspaceMember currentMember)
        {
            // Owner و Admin مفيش قيد عليهم — القيد ده بيتطبق على Member بس.
            if (currentMember.Role != WorkspaceRole.Member)
            {
                return Task.FromResult(true);
            }

            var isToDo = _task.Status == TaskItemStatus.Todo;
            var isCreator = _task.CreatedByWorkspaceMemberId == currentMember.Id;

            return Task.FromResult(isToDo && isCreator);
        }
    }
}
