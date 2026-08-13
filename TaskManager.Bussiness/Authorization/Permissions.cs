namespace TaskManager.Bussiness.Authorization
{
    /// <summary>
    /// Workspace Permission Catalog (Static — V1).
    /// المصدر الوحيد للصلاحيات بجانب WorkspaceMember.Role + RolePermissionCatalog.
    /// لا يخلط مع Legacy catalog (TaskManager.API.Authorization.Permissions) —
    /// الاتنين منفصلين لغاية ما التعميم يكمل.
    /// إجمالي: 35 Permission موزعة على 9 Resources.
    /// </summary>
    public static class Permissions
    {
        // ── Workspace ───────────────────────────────────────────────────────
        public const string WorkspaceView = "Workspace.View";
        public const string WorkspaceUpdate = "Workspace.Update";
        public const string WorkspaceArchive = "Workspace.Archive";
        public const string WorkspaceTransferOwnership = "Workspace.TransferOwnership";

        // ── Members ─────────────────────────────────────────────────────────
        public const string MembersView = "Members.View";
        public const string MembersInvite = "Members.Invite";
        public const string MembersRemove = "Members.Remove";
        public const string MembersChangeRole = "Members.ChangeRole";
        public const string MembersSuspend = "Members.Suspend";

        // ── Invitations ─────────────────────────────────────────────────────
        public const string InvitationsView = "Invitations.View";
        public const string InvitationsCancel = "Invitations.Cancel";
        public const string InvitationsResend = "Invitations.Resend";

        // ── Teams ───────────────────────────────────────────────────────────
        public const string TeamsCreate = "Teams.Create";
        public const string TeamsUpdate = "Teams.Update";
        public const string TeamsDelete = "Teams.Delete";
        public const string TeamsManageMembers = "Teams.ManageMembers";

        // ── Projects ────────────────────────────────────────────────────────
        public const string ProjectsCreate = "Projects.Create";
        public const string ProjectsUpdate = "Projects.Update";
        public const string ProjectsArchive = "Projects.Archive";
        public const string ProjectsManageMembers = "Projects.ManageMembers";
        public const string ProjectsManageTeams = "Projects.ManageTeams";

        // ── Tasks ───────────────────────────────────────────────────────────
        public const string TasksCreate = "Tasks.Create";
        public const string TasksUpdate = "Tasks.Update";
        public const string TasksAssign = "Tasks.Assign";
        public const string TasksChangeStatus = "Tasks.ChangeStatus";
        public const string TasksChangePriority = "Tasks.ChangePriority";
        public const string TasksDelete = "Tasks.Delete";
        public const string TasksViewTrash = "Tasks.ViewTrash";
        public const string TasksRestore = "Tasks.Restore";

        // ── Comments ────────────────────────────────────────────────────────
        public const string CommentsCreate = "Comments.Create";
        public const string CommentsUpdate = "Comments.Update";
        public const string CommentsDelete = "Comments.Delete";

        // ── Attachments ─────────────────────────────────────────────────────
        public const string AttachmentsUpload = "Attachments.Upload";
        public const string AttachmentsView = "Attachments.View";
        public const string AttachmentsDelete = "Attachments.Delete";

        // ── AuditLog ────────────────────────────────────────────────────────
        public const string AuditLogView = "AuditLog.View";

        public static readonly string[] All =
        {
            WorkspaceView, WorkspaceUpdate, WorkspaceArchive, WorkspaceTransferOwnership,

            MembersView, MembersInvite, MembersRemove, MembersChangeRole, MembersSuspend,

            InvitationsView, InvitationsCancel, InvitationsResend,

            TeamsCreate, TeamsUpdate, TeamsDelete, TeamsManageMembers,

            ProjectsCreate, ProjectsUpdate, ProjectsArchive, ProjectsManageMembers, ProjectsManageTeams,

            TasksCreate, TasksUpdate, TasksAssign, TasksChangeStatus, TasksChangePriority,
            TasksDelete, TasksViewTrash, TasksRestore,

            CommentsCreate, CommentsUpdate, CommentsDelete,

            AttachmentsUpload, AttachmentsView, AttachmentsDelete,

            AuditLogView
        };
    }
}
