using TaskManager.Data.Enums;

namespace TaskManager.Bussiness.Authorization
{
    /// <summary>
    /// Role → Permission Matrix (الثابت الوحيد — Static in code, مش DB).
    /// أي تعديل على الـ Matrix يتم هنا فقط + توثيقه في وثيقة Authorization المعتمدة.
    /// Resource Conditions (*) تتطبق من الـ Pipeline على مستواها — الماتركس ده بيحدد
    /// صلاحية الـ Role الأساسية بس (بدون القيود المشروطة).
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
                    // Workspace (بدون امتيازات الملكية)
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
                    // Tasks / Comments / Attachments فقط (معظمها مقيدة بشروط الـ Resource نفسه)
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

        /// <summary>هل الـ Role عنده الـ Permission المطلوبة في الـ Catalog؟</summary>
        public static bool HasPermission(WorkspaceRole role, string permission) =>
            Mapping.TryGetValue(role, out var permissions) && permissions.Contains(permission);

        /// <summary>كل الصلاحيات بتاعة الـ Role ده (للتقارير/الـ UI).</summary>
        public static IReadOnlySet<string> GetPermissions(WorkspaceRole role) =>
            Mapping.TryGetValue(role, out var permissions)
                ? permissions
                : (IReadOnlySet<string>)new HashSet<string>();
    }
}
