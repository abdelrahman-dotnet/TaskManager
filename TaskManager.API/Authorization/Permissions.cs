namespace TaskManager.API.Authorization
{
    public static class Permissions
    {
        // ── Projects ─────────────────────────────────────────────────────────
        // No "ownership" concept at this level (a whole project isn't personal),
        // so these gate the endpoint directly.
        public const string ProjectsCreate = "Projects.Create";
        public const string ProjectsUpdate = "Projects.Update";
        public const string ProjectsDelete = "Projects.Delete";
        // NEW (Membership System): gates the Management API endpoints
        // (POST/DELETE/PATCH /projects/{id}/members). Deliberately separate from
        // ProjectsUpdate - editing a project's own fields and managing who belongs to it
        // are different responsibilities. The Service still enforces
        // EnsureCanManageProjectAsync (Membership) on top of this (Permission && Membership).
        public const string ProjectsManageMembers = "Projects.ManageMembers";

        // ── Tasks ─────────────────────────────────────────────────────────────
        // Create/Assign have no ownership concept (you're acting on someone
        // else's work by definition) -> gate the endpoint directly.
        public const string TasksCreate = "Tasks.Create";
        public const string TasksAssign = "Tasks.Assign";
        // Update/Delete are "base" permissions: granted broadly (incl. plain
        // Users) so people can edit/delete tasks *they created or are
        // assigned to*. The Service still enforces ownership - this only
        // gates whether the endpoint can be called at all.
        public const string TasksUpdate = "Tasks.Update";
        public const string TasksDelete = "Tasks.Delete";
        // Elevated bypass: lets the holder act on ANY task regardless of who
        // created/is assigned to it. Passed to the Service as the
        // "isAdmin"-style override flag - do NOT hand this to plain Users,
        // or the ownership check becomes meaningless for everyone.
        public const string TasksManageAny = "Tasks.ManageAny";

        // ── Comments ──────────────────────────────────────────────────────────
        public const string CommentsCreate = "Comments.Create";
        public const string CommentsUpdate = "Comments.Update";
        public const string CommentsDelete = "Comments.Delete";
        public const string CommentsManageAny = "Comments.ManageAny";

        // ── Attachments ───────────────────────────────────────────────────────
        public const string AttachmentsCreate = "Attachments.Create";
        public const string AttachmentsManageAny = "Attachments.ManageAny";

        // ── Teams ─────────────────────────────────────────────────────────────
        public const string TeamsCreate = "Teams.Create";
        public const string TeamsUpdate = "Teams.Update";
        public const string TeamsDelete = "Teams.Delete";
        // NEW (Membership System): gates the Management API endpoints
        // (POST/DELETE/PATCH /teams/{id}/members). Same reasoning as
        // ProjectsManageMembers above - separate from TeamsUpdate on purpose.
        public const string TeamsManageMembers = "Teams.ManageMembers";

        // ── Users ─────────────────────────────────────────────────────────────
        public const string UsersView = "Users.View";
        public const string UsersCreate = "Users.Create";
        public const string UsersManageStatus = "Users.ManageStatus";
        public const string UsersDelete = "Users.Delete";
        public const string UsersManageRoles = "Users.ManageRoles";
        // Elevated bypass for UserController.Update - same idea as
        // Tasks.ManageAny: lets the holder edit ANY user's profile, not just
        // their own. Do NOT hand this to plain Users.
        public const string UsersManageAny = "Users.ManageAny";

        // ── Roles ─────────────────────────────────────────────────────────────
        public const string RolesManage = "Roles.Manage";

        // ── Reporting / oversight (read-only) ────────────────────────────────
        public const string TaskAssignmentsView = "TaskAssignments.View";
        public const string TaskStatusHistoryView = "TaskStatusHistory.View";
        public const string AuditLogsView = "AuditLogs.View";

        public static readonly string[] All =
        {
            ProjectsCreate, ProjectsUpdate, ProjectsDelete, ProjectsManageMembers,

            TasksCreate, TasksAssign, TasksUpdate, TasksDelete, TasksManageAny,

            CommentsCreate, CommentsUpdate, CommentsDelete, CommentsManageAny,

            AttachmentsCreate, AttachmentsManageAny,

            TeamsCreate, TeamsUpdate, TeamsDelete, TeamsManageMembers,

            UsersView, UsersCreate, UsersManageStatus, UsersDelete, UsersManageRoles, UsersManageAny,

            RolesManage,

            TaskAssignmentsView, TaskStatusHistoryView, AuditLogsView
        };
    }
}
