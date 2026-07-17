using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using TaskManager.API.Enums.SortingFields;
using TaskManager.Data.Entities;

namespace TaskManager.API.Config
{
    public static class AllowedSortingFields
    {
        // TaskItem
        public static readonly Dictionary<TaskSortingFields, Expression<Func<TaskItem, object>>> Tasks = new()
        {
            [TaskSortingFields.Id] = x => x.Id,
            [TaskSortingFields.Title] = x => x.Title,
            [TaskSortingFields.Description] = x => x.Description!,
            [TaskSortingFields.Status] = x => x.Status,
            [TaskSortingFields.Priority] = x => x.Priority,
            [TaskSortingFields.DueDate] = x => x.DueDate!,
            [TaskSortingFields.CompletedAt] = x => x.CompletedAt!,
            [TaskSortingFields.ProjectId] = x => x.ProjectId!,
            [TaskSortingFields.CreatedByUserId] = x => x.CreatedByUserId,
            [TaskSortingFields.CreatedAt] = x => x.CreatedAt,
            [TaskSortingFields.UpdatedAt] = x => x.UpdatedAt!
        };

        // ApplicationUser
        public static readonly Dictionary<UserSortingFields, Expression<Func<ApplicationUser, object>>> Users = new()
        {
            [UserSortingFields.Id] = x => x.Id,
            [UserSortingFields.UserName] = x => x.UserName,
            [UserSortingFields.IsActive] = x => x.IsActive,
            [UserSortingFields.TeamId] = x => x.TeamId!,
            [UserSortingFields.LastLoginAt] = x => x.LastLoginAt!,
            [UserSortingFields.CreatedAt] = x => x.CreatedAt
        };

        // Comment (زي ما هي، مفيش تغيير)
        public static readonly Dictionary<CommentSortingFields, Expression<Func<Comment, object>>> Comments = new()
        {
            [CommentSortingFields.Id] = x => x.Id,
            [CommentSortingFields.Content] = x => x.Content,
            [CommentSortingFields.CreatedAt] = x => x.CreatedAt,
            [CommentSortingFields.TaskItemId] = x => x.TaskItemId,
            [CommentSortingFields.UserId] = x => x.UserId
        };

        // Role
        public static readonly Dictionary<RoleSortingFields, Expression<Func<ApplicationRole, object>>> Roles = new()
        {
            [RoleSortingFields.Id] = x => x.Id,
            [RoleSortingFields.Name] = x => x.Name!,
            [RoleSortingFields.Description] = x => x.Description!
        };
        // Project
        public static readonly Dictionary<ProjectSortingFields, Expression<Func<Project, object>>> Projects = new()
        {
            [ProjectSortingFields.Id] = x => x.Id,
            [ProjectSortingFields.Name] = x => x.Name,
            [ProjectSortingFields.IsArchived] = x => x.IsArchived,
            [ProjectSortingFields.StartDate] = x => x.StartDate!,
            [ProjectSortingFields.EndDate] = x => x.EndDate!,
            [ProjectSortingFields.TeamId] = x => x.TeamId,
            [ProjectSortingFields.CreatedByUserId] = x => x.CreatedByUserId,
            [ProjectSortingFields.CreatedAt] = x => x.CreatedAt
        };

        // Team
        public static readonly Dictionary<TeamSortingFields, Expression<Func<Team, object>>> Teams = new()
        {
            [TeamSortingFields.Id] = x => x.Id,
            [TeamSortingFields.Name] = x => x.Name,
            [TeamSortingFields.ManagerId] = x => x.ManagerId,
            [TeamSortingFields.CreatedAt] = x => x.CreatedAt
        };

        // Notification
        public static readonly Dictionary<NotificationSortingFields, Expression<Func<Notification, object>>> Notifications = new()
        {
            [NotificationSortingFields.Id] = x => x.Id,
            [NotificationSortingFields.Title] = x => x.Title,
            [NotificationSortingFields.IsRead] = x => x.IsRead,
            [NotificationSortingFields.UserId] = x => x.UserId,
            [NotificationSortingFields.CreatedAt] = x => x.CreatedAt
        };

        // Attachment
        public static readonly Dictionary<AttachmentSortingFields, Expression<Func<Attachment, object>>> Attachments = new()
        {
            [AttachmentSortingFields.Id] = x => x.Id,
            [AttachmentSortingFields.FileName] = x => x.FileName,
            [AttachmentSortingFields.FileSize] = x => x.FileSize,
            [AttachmentSortingFields.ContentType] = x => x.ContentType,
            [AttachmentSortingFields.UploadedByUserId] = x => x.UploadedByUserId,
            [AttachmentSortingFields.CreatedAt] = x => x.CreatedAt
        };

        // TaskStatusHistory
        public static readonly Dictionary<TaskStatusHistorySortingFields, Expression<Func<TaskStatusHistory, object>>> TaskStatusHistories = new()
        {
            [TaskStatusHistorySortingFields.Id] = x => x.Id,
            [TaskStatusHistorySortingFields.TaskItemId] = x => x.TaskItemId,
            [TaskStatusHistorySortingFields.OldStatus] = x => x.OldStatus,
            [TaskStatusHistorySortingFields.NewStatus] = x => x.NewStatus,
            [TaskStatusHistorySortingFields.ChangedByUserId] = x => x.ChangedByUserId,
            [TaskStatusHistorySortingFields.ChangedAt] = x => x.ChangedAt
        };

        // AuditLog
        public static readonly Dictionary<AuditLogSortingFields, Expression<Func<AuditLog, object>>> AuditLogs = new()
        {
            [AuditLogSortingFields.Id] = x => x.Id,
            [AuditLogSortingFields.Action] = x => x.Action,
            [AuditLogSortingFields.EntityName] = x => x.EntityName,
            [AuditLogSortingFields.UserId] = x => x.UserId!,
            [AuditLogSortingFields.CreatedAt] = x => x.CreatedAt
        };

        // TaskAssignment
        public static readonly Dictionary<TaskAssignmentSortingFields, Expression<Func<TaskAssignment, object>>> TaskAssignments = new()
        {
            [TaskAssignmentSortingFields.Id] = x => x.Id,
            [TaskAssignmentSortingFields.TaskItemId] = x => x.TaskItemId,
            [TaskAssignmentSortingFields.UserId] = x => x.UserId,
            [TaskAssignmentSortingFields.AssignedByUserId] = x => x.AssignedByUserId,
            [TaskAssignmentSortingFields.AssignedAt] = x => x.AssignedAt
        };
    }
}