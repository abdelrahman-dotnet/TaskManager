using TaskManager.Data.Enums;

namespace TaskManager.Bussiness.Authorization
{
    /// <summary>
    /// Role â†’ Permission Matrix (Ø§Ù„Ø«Ø§Ø¨Øª Ø§Ù„ÙˆØ­ÙŠØ¯ â€” Static in code, Ù…Ø´ DB).
    /// Ø£ÙŠ ØªØ¹Ø¯ÙŠÙ„ Ø¹Ù„Ù‰ Ø§Ù„Ù€ Matrix ÙŠØªÙ… Ù‡Ù†Ø§ ÙÙ‚Ø· + ØªÙˆØ«ÙŠÙ‚Ù‡ ÙÙŠ ÙˆØ«ÙŠÙ‚Ø© Authorization Ø§Ù„Ù…Ø¹ØªÙ…Ø¯Ø©.
    /// Resource Conditions (*) ØªØªØ·Ø¨Ù‚ Ù…Ù† Ø§Ù„Ù€ Pipeline Ø¹Ù„Ù‰ Ù…Ø³ØªÙˆØ§Ù‡Ø§ â€” Ø§Ù„Ù…Ø§ØªØ±ÙƒØ³ Ø¯Ù‡ Ø¨ÙŠØ­Ø¯Ø¯
    /// ØµÙ„Ø§Ø­ÙŠØ© Ø§Ù„Ù€ Role Ø§Ù„Ø£Ø³Ø§Ø³ÙŠØ© Ø¨Ø³ (Ø¨Ø¯ÙˆÙ† Ø§Ù„Ù‚ÙŠÙˆØ¯ Ø§Ù„Ù…Ø´Ø±ÙˆØ·Ø©).
    /// </summary>
    public static class RolePermissionCatalog
    {
        private static readonly IReadOnlyDictionary<WorkspaceRole, IReadOnlySet<string>> Mapping =
            new Dictionary<WorkspaceRole, IReadOnlySet<string>>
            {
                [WorkspaceRole.Owner] = new HashSet<string>(StringComparer.Ordinal)
                {
                    // Workspace: Owner only for ownership privileges
                    Permissions.WorkspaceView,
                    Permissions.WorkspaceUpdate,
                    Permissions.WorkspaceArchive,
                    Permissions.WorkspaceTransferOwnership,

                    // Members / Invitations
                    Permissions.MembersView,
                    Permissions.MembersInvite,
                    Permissions.MembersRemove,
                    Permissions.MembersChangeRole,
                    Permissions.MembersSuspend,
                    Permissions.InvitationsView,
                    Permissions.InvitationsCancel,
                    Permissions.InvitationsResend,

                    // Teams / Projects
                    Permissions.TeamsCreate,
                    Permissions.TeamsUpdate,
                    Permissions.TeamsDelete,
                    Permissions.TeamsManageMembers,
                    Permissions.ProjectsCreate,
                    Permissions.ProjectsUpdate,
                    Permissions.ProjectsDelete,
                    Permissions.ProjectsArchive,
                    Permissions.ProjectsManageMembers,
                    Permissions.ProjectsManageTeams,

                    // Tasks / Comments / Attachments / Audit
                    Permissions.TasksCreate,
                    Permissions.TasksUpdate,
                    Permissions.TasksAssign,
                    Permissions.TasksChangeStatus,
                    Permissions.TasksChangePriority,
                    Permissions.TasksDelete,
                    Permissions.TasksViewTrash,
                    Permissions.TasksRestore,
                    Permissions.CommentsCreate,
                    Permissions.CommentsUpdate,
                    Permissions.CommentsDelete,
                    Permissions.AttachmentsUpload,
                    Permissions.AttachmentsView,
                    Permissions.AttachmentsDelete,
                    Permissions.AuditLogView,
                },

                [WorkspaceRole.Admin] = new HashSet<string>(StringComparer.Ordinal)
                {
                    // Workspace (Ø¨Ø¯ÙˆÙ† Ø§Ù…ØªÙŠØ§Ø²Ø§Øª Ø§Ù„Ù…Ù„ÙƒÙŠØ©)
                    Permissions.WorkspaceView,
                    Permissions.WorkspaceUpdate,

                    // Members / Invitations
                    Permissions.MembersView,
                    Permissions.MembersInvite,
                    Permissions.MembersRemove,
                    Permissions.MembersChangeRole,
                    Permissions.MembersSuspend,
                    Permissions.InvitationsView,
                    Permissions.InvitationsCancel,
                    Permissions.InvitationsResend,

                    // Teams / Projects
                    Permissions.TeamsCreate,
                    Permissions.TeamsUpdate,
                    Permissions.TeamsDelete,
                    Permissions.TeamsManageMembers,
                    Permissions.ProjectsCreate,
                    Permissions.ProjectsUpdate,
                    Permissions.ProjectsDelete,
                    Permissions.ProjectsArchive,
                    Permissions.ProjectsManageMembers,
                    Permissions.ProjectsManageTeams,

                    // Tasks / Comments / Attachments / Audit
                    Permissions.TasksCreate,
                    Permissions.TasksUpdate,
                    Permissions.TasksAssign,
                    Permissions.TasksChangeStatus,
                    Permissions.TasksChangePriority,
                    Permissions.TasksDelete,
                    Permissions.TasksViewTrash,
                    Permissions.TasksRestore,
                    Permissions.CommentsCreate,
                    Permissions.CommentsUpdate,
                    Permissions.CommentsDelete,
                    Permissions.AttachmentsUpload,
                    Permissions.AttachmentsView,
                    Permissions.AttachmentsDelete,
                    Permissions.AuditLogView,
                },

                [WorkspaceRole.Member] = new HashSet<string>(StringComparer.Ordinal)
                {
                    // Tasks / Comments / Attachments ÙÙ‚Ø· (Ù…Ø¹Ø¸Ù…Ù‡Ø§ Ù…Ù‚ÙŠØ¯Ø© Ø¨Ø´Ø±ÙˆØ· Ø§Ù„Ù€ Resource Ù†ÙØ³Ù‡)
                    Permissions.TasksCreate,
                    Permissions.TasksUpdate,
                    Permissions.TasksAssign,
                    Permissions.TasksChangeStatus,
                    Permissions.TasksChangePriority,
                    Permissions.TasksDelete,
                    Permissions.CommentsCreate,
                    Permissions.CommentsUpdate,
                    Permissions.CommentsDelete,
                    Permissions.AttachmentsUpload,
                    Permissions.AttachmentsView,
                },
            };

        /// <summary>Ù‡Ù„ Ø§Ù„Ù€ Role Ø¹Ù†Ø¯Ù‡ Ø§Ù„Ù€ Permission Ø§Ù„Ù…Ø·Ù„ÙˆØ¨Ø© ÙÙŠ Ø§Ù„Ù€ CatalogØŸ</summary>
        public static bool HasPermission(WorkspaceRole role, string permission) =>
            Mapping.TryGetValue(role, out var permissions) && permissions.Contains(permission);

        /// <summary>ÙƒÙ„ Ø§Ù„ØµÙ„Ø§Ø­ÙŠØ§Øª Ø¨ØªØ§Ø¹Ø© Ø§Ù„Ù€ Role Ø¯Ù‡ (Ù„Ù„ØªÙ‚Ø§Ø±ÙŠØ±/Ø§Ù„Ù€ UI).</summary>
        public static IReadOnlySet<string> GetPermissions(WorkspaceRole role) =>
            Mapping.TryGetValue(role, out var permissions)
                ? permissions
                : (IReadOnlySet<string>)new HashSet<string>();
    }
}
