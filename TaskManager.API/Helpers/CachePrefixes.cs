namespace TaskManager.Bussiness.Caching
{
    public static class CachePrefixes
    {
        #region Tasks
        public const string TasksList = "Tasks:List";
        public const string TasksSearch = "Tasks:Search";
        public const string TasksWithComments = "Tasks:WithComments";
        public const string TaskById = "Tasks:ById";
        public const string TaskByIdWithIncludes = "Tasks:ByIdWithIncludes";
        #endregion

        #region Roles
        public const string RolesList = "Roles:List";
        public const string RoleById = "Roles:ById";
        public const string UserRoles = "Roles:UserRoles";
        #endregion

        #region Comments
        public const string CommentsList = "Comments:List";
        public const string CommentsByTask = "Comments:ByTask";
        #endregion

        #region Users
        public const string UsersList = "Users:List";
        public const string UsersWithTasks = "Users:WithTasks";
        public const string UsersWithComments = "Users:WithComments";
        public const string UserById = "Users:ById";
        #endregion

        #region Projects
        public const string ProjectsList = "Projects:List";
        public const string ProjectById = "Projects:ById";
        #endregion

        #region Teams
        public const string TeamsList = "Teams:List";
        public const string TeamById = "Teams:ById";
        #endregion

        #region Attachments
        public const string AttachmentsList = "Attachments:List";
        public const string AttachmentsByTask = "Attachments:ByTask";
        #endregion

        #region Notifications
        public const string NotificationsList = "Notifications:List";
        public const string NotificationsByUser = "Notifications:ByUser";
        #endregion

        #region AuditLogs
        public const string AuditLogsList = "AuditLogs:List";
        #endregion

        #region TaskAssignments
        public const string TaskAssignmentsList = "TaskAssignments:List";
        #endregion

        #region TaskStatusHistories
        public const string TaskStatusHistoriesList = "TaskStatusHistories:List";
        #endregion
    }
}
