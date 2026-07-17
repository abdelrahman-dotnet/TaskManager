namespace TaskManager.API.DTOs.Dashboard
{
    public class DashboardOverviewDto
    {
        public int TotalProjects { get; set; }

        public int TotalTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int OverdueTasks { get; set; }

        public int TotalTeams { get; set; }

        public int TotalUsers { get; set; }
    }
}
