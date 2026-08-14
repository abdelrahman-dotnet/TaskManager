namespace TaskManager.Bussiness.Authorization
{
    /// <summary>
    /// Workspace Permission Catalog (Static â€” V1).
    /// Ø§Ù„Ù…ØµØ¯Ø± Ø§Ù„ÙˆØ­ÙŠØ¯ Ù„Ù„ØµÙ„Ø§Ø­ÙŠØ§Øª Ø¨Ø¬Ø§Ù†Ø¨ WorkspaceMember.Role + RolePermissionCatalog.
    /// Ù„Ø§ ÙŠØ®Ù„Ø· Ù…Ø¹ Legacy catalog (TaskManager.API.Authorization.Permissions) â€”
    /// Ø§Ù„Ø§ØªÙ†ÙŠÙ† Ù…Ù†ÙØµÙ„ÙŠÙ† Ù„ØºØ§ÙŠØ© Ù…Ø§ Ø§Ù„ØªØ¹Ù…ÙŠÙ… ÙŠÙƒÙ…Ù„.
    /// Ø¥Ø¬Ù…Ø§Ù„ÙŠ: 35 Permission Ù…ÙˆØ²Ø¹Ø© Ø¹Ù„Ù‰ 9 Resources.
    /// </summary>
    public static class Permissions
    {
        // â”€â”€ Workspace â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public const string WorkspaceView = "Workspace.View";
        public const string WorkspaceUpdate = "Workspace.Update";
        public const string WorkspaceArchive = "Workspace.Archive";
        public const string WorkspaceTransferOwnership = "Workspace.TransferOwnership";

        // â”€â”€ Members â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public const string MembersView = "Members.View";
        public const string MembersInvite = "Members.Invite";
        public const string MembersRemove = "Members.Remove";
        public const string MembersChangeRole = "Members.ChangeRole";
        public const string MembersSuspend = "Members.Suspend";

        // â”€â”€ Invitations â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public const string InvitationsView = "Invitations.View";
        public const string InvitationsCancel = "Invitations.Cancel";
        public const string InvitationsResend = "Invitations.Resend";

        // â”€â”€ Teams â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public const string TeamsCreate = "Teams.Create";
        public const string TeamsUpdate = "Teams.Update";
        public const string TeamsDelete = "Teams.Delete";
        public const string TeamsManageMembers = "Teams.ManageMembers";

        // â”€â”€ Projects â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public const string ProjectsCreate = "Projects.Create";
        public const string ProjectsUpdate = "Projects.Update";
        // Projects.Delete was removed per G-2 (V1 scope reduction): Project lifecycle
        // is Archive / Restore only. Project deletion is NOT part of V1.
        public const string ProjectsArchive = "Projects.Archive";
        public const string ProjectsManageMembers = "Projects.ManageMembers";
        public const string ProjectsManageTeams = "Projects.ManageTeams";

        // â”€â”€ Tasks â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public const string TasksCreate = "Tasks.Create";
        public const string TasksUpdate = "Tasks.Update";
        public const string TasksAssign = "Tasks.Assign";
        public const string TasksChangeStatus = "Tasks.ChangeStatus";
        public const string TasksChangePriority = "Tasks.ChangePriority";
        public const string TasksDelete = "Tasks.Delete";
        public const string TasksViewTrash = "Tasks.ViewTrash";
        public const string TasksArchive = "Tasks.Archive";
        public const string TasksRestore = "Tasks.Restore";

        // â”€â”€ Comments â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public const string CommentsCreate = "Comments.Create";
        public const string CommentsUpdate = "Comments.Update";
        public const string CommentsDelete = "Comments.Delete";

        // â”€â”€ Attachments â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public const string AttachmentsUpload = "Attachments.Upload";
        public const string AttachmentsView = "Attachments.View";
        public const string AttachmentsDelete = "Attachments.Delete";

        // â”€â”€ AuditLog â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public const string AuditLogView = "AuditLog.View";

        public static readonly string[] All =
        {
            WorkspaceView, WorkspaceUpdate, WorkspaceArchive, WorkspaceTransferOwnership,

            MembersView, MembersInvite, MembersRemove, MembersChangeRole, MembersSuspend,

            InvitationsView, InvitationsCancel, InvitationsResend,

            TeamsCreate, TeamsUpdate, TeamsDelete, TeamsManageMembers,

            ProjectsCreate, ProjectsUpdate, ProjectsArchive, ProjectsManageMembers, ProjectsManageTeams,

            TasksCreate, TasksUpdate, TasksAssign, TasksChangeStatus, TasksChangePriority,
            TasksDelete, TasksViewTrash, TasksArchive, TasksRestore,

            CommentsCreate, CommentsUpdate, CommentsDelete,

            AttachmentsUpload, AttachmentsView, AttachmentsDelete,

            AuditLogView
        };
    }
}
