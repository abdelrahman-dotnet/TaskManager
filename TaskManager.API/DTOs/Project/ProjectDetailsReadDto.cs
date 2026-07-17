namespace TaskManager.API.DTOs.Project
{
    public class ProjectDetailsReadDto : ProjectReadDto
    {
        public int TasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
    }
}
