namespace TaskManager.Data.Entities
{
    // Renamed from "TaskStatus" -> "TaskItemStatus".
    // "TaskStatus" collides with System.Threading.Tasks.TaskStatus and will
    // cause ambiguous-reference errors the moment someone adds
    // "using System.Threading.Tasks;" in a file that also uses this enum.
    public enum TaskItemStatus
    {
        Todo = 1,
        InProgress = 2,
        InReview = 3,
        Done = 4,
        Cancelled = 5
    }
}
