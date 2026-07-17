using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<TaskStatusHistory> TaskStatusHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ─── ApplicationUser And Team
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(u => u.TeamId)
                .OnDelete(DeleteBehavior.SetNull);  // mean set child null

            // ─── Team ─ Manager(User)
            builder.Entity<Team>()
                .HasOne(t => t.Manager)
                .WithMany(u => u.ManagedTeams)
                .HasForeignKey(t => t.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── Project ─ Creator
            builder.Entity<Project>()
                .HasOne(p => p.CreatedByUser)
                .WithMany(u => u.CreatedProjects)
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── Project ─ Team
            // TeamId required, لازم تتعرف صراحة وإلا EF Core بالـ Convention هيحطها Cascade،
            // وده هيتعارض مع Restrict بتاعة TaskItem->Project ويطلع خطأ
            // "multiple cascade paths" أول ما تعمل Migration فعلي على SQL Server.
            builder.Entity<Project>()
                .HasOne(p => p.Team)
                .WithMany(t => t.Projects)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // ─── TaskItem ─ Creator - Project
            builder.Entity<TaskItem>(entity =>
            {
                entity.Property(t => t.Status)
                    .HasConversion<string>();

                entity.Property(t => t.Priority)
                    .HasConversion<string>();

                entity.HasOne(t => t.CreatedByUser)
                    .WithMany(u => u.CreatedTasks)
                    .HasForeignKey(t => t.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.Entity<TaskItem>()
                    .HasOne(t => t.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(t => t.ProjectId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.Priority);
                entity.HasIndex(t => t.DueDate);
                entity.HasIndex(t => t.CreatedByUserId);
                entity.HasIndex(t => t.ProjectId);
            });

            // ─── Comment ─ TaskItem - User
            builder.Entity<Comment>(entity =>
            {
                entity.HasOne(c => c.TaskItem)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(c => c.TaskItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(c => c.TaskItemId);
            });

            // ─── TaskAssignment ── TaskAssignment - AssignedToUser - Created by user
            builder.Entity<TaskAssignment>(entity =>
            {
                entity.HasOne(ta => ta.TaskItem)
                    .WithMany(t => t.Assignments)
                    .HasForeignKey(ta => ta.TaskItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ta => ta.User)
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(ta => ta.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ta => ta.AssignedByUser)
                    .WithMany(u => u.CreatedAssignments)
                    .HasForeignKey(ta => ta.AssignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── TaskStatusHistory ─ TaskItem - User
            builder.Entity<TaskStatusHistory>(entity =>
            {
                entity.Property(h => h.OldStatus)
                    .HasConversion<string>();

                entity.Property(h => h.NewStatus)
                    .HasConversion<string>();

                entity.HasOne(h => h.TaskItem)
                    .WithMany(t => t.StatusHistory)
                    .HasForeignKey(h => h.TaskItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.ChangedByUser)
                    .WithMany(u => u.StatusChanges)
                    .HasForeignKey(h => h.ChangedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(h => h.TaskItemId);
                entity.HasIndex(h => h.ChangedAt);
            });

            // ─── Notification ─ User
            builder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(n => new { n.UserId, n.IsRead });
            });

            // ─── AuditLog ── User
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasOne(a => a.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(a => new { a.EntityName, a.EntityId });
                entity.HasIndex(a => a.CreatedAt);
            });

            // ─── Attachment ─ Task - User
            builder.Entity<Attachment>(entity =>
            {
                entity.HasOne(a => a.TaskItem)
                    .WithMany(t => t.Attachments)
                    .HasForeignKey(a => a.TaskItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.UploadedByUser)
                    .WithMany(u => u.UploadedAttachments)
                    .HasForeignKey(a => a.UploadedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(a => a.TaskItemId);
            });

            // ─── Permission ───────────────────────────────────────────────────────
            builder.Entity<Permission>(entity =>
            {
                entity.HasIndex(p => p.Name)
                    .IsUnique();
            });

            // ─── RolePermission ───────────────────────────────────────────────────
            builder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rp => new
                {
                    rp.RoleId,
                    rp.PermissionId
                });

                entity.HasOne(rp => rp.Role)
                    .WithMany(rp => rp.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId);

                entity.HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId);
            });
        }
    }
}