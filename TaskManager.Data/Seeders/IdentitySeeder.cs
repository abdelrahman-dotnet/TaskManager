using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;

public static class PermissionAndRoleSeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        AppDbContext context)
    {
        await SeedRoles(roleManager);
        await SeedPermissions(context);
        await SeedRolePermissions(roleManager, context);
        await SeedAdminUser(userManager);
    }

    // =========================
    // ROLES
    // =========================
    private static async Task SeedRoles(RoleManager<ApplicationRole> roleManager)
    {
        string[] roles = { "Admin", "Manager", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = role
                });
            }
        }
    }

    // =========================
    // PERMISSIONS
    //
    // NOTE: this list must be kept in sync, by name, with
    // TaskManager.API.Authorization.Permissions.All. It's duplicated here
    // (rather than referencing that class directly) because TaskManager.Data
    // cannot take a project reference on TaskManager.API without creating a
    // circular dependency. If you add/rename/remove a permission in
    // Permissions.cs, mirror the change here too, and vice versa -
    // this exact mismatch ("permissions made for the old system") is what
    // silently broke authorization before: policies existed for permissions
    // no role ever actually got.
    // =========================
    private static readonly string[] AllPermissions =
    {
        // Projects - no ownership concept, gates the endpoint directly
        "Projects.Create",
        "Projects.Update",
        "Projects.Delete",
        "Projects.ManageMembers",

        // Tasks - Create/Assign have no ownership concept; Update/Delete are
        // "base" (you can touch your own); ManageAny bypasses ownership
        "Tasks.Create",
        "Tasks.Assign",
        "Tasks.Update",
        "Tasks.Delete",
        "Tasks.ManageAny",

        // Comments - same base/ManageAny split as Tasks
        "Comments.Create",
        "Comments.Update",
        "Comments.Delete",
        "Comments.ManageAny",

        // Attachments - same base/ManageAny split
        "Attachments.Create",
        "Attachments.ManageAny",

        // Teams
        "Teams.Create",
        "Teams.Update",
        "Teams.Delete",
        "Teams.ManageMembers",

        // Users
        "Users.View",
        "Users.Create",
        "Users.ManageStatus",
        "Users.Delete",
        "Users.ManageRoles",

        // Roles
        "Roles.Manage",

        // Reporting / oversight (read-only)
        "TaskAssignments.View",
        "TaskStatusHistory.View",
        "AuditLogs.View",
    };

    private static async Task SeedPermissions(AppDbContext context)
    {
        var existingNames = await context.Permissions
            .Select(p => p.Name)
            .ToListAsync();

        // FIX: previously this bailed out entirely ("if any permission
        // exists, return") the moment a single permission had ever been
        // seeded - so adding new permissions later (Teams/Users/Roles/
        // Attachments/reporting, all added in this pass) would silently
        // never get inserted on an existing database. Now it inserts only
        // the ones that are actually missing, so growing the permission set
        // over time works correctly.
        var missing = AllPermissions
            .Where(name => !existingNames.Contains(name))
            .Select(name => new Permission { Name = name })
            .ToList();

        if (missing.Count == 0)
            return;

        await context.Permissions.AddRangeAsync(missing);
        await context.SaveChangesAsync();
    }

    // =========================
    // ROLE PERMISSIONS
    // =========================
    private static async Task SeedRolePermissions(
        RoleManager<ApplicationRole> roleManager,
        AppDbContext context)
    {
        var admin = await roleManager.FindByNameAsync("Admin");
        var manager = await roleManager.FindByNameAsync("Manager");
        var user = await roleManager.FindByNameAsync("User");

        var permissions = await context.Permissions.ToListAsync();

        // Admin = ALL permissions, always kept in sync (adding a permission
        // later automatically grants it to Admin without touching this file).
        await SyncRolePermissions(context, admin!.Id, permissions,
            permissions.Select(p => p.Name).ToArray());

        // Manager: can run day-to-day project/task work for their team, and
        // oversee it, but can't delete projects, manage teams'
        // existence/roles, or touch user/role administration.
        string[] managerPerms =
        {
            "Projects.Create",
            "Projects.Update",
            "Projects.ManageMembers",

            "Tasks.Create",
            "Tasks.Assign",
            "Tasks.Update",
            "Tasks.Delete",
            "Tasks.ManageAny",

            "Comments.Create",
            "Comments.Update",
            "Comments.Delete",
            "Comments.ManageAny",

            "Attachments.Create",
            "Attachments.ManageAny",

            "Teams.Update",
            "Teams.ManageMembers",

            "Users.View",

            "TaskAssignments.View",
            "TaskStatusHistory.View",
        };
        await SyncRolePermissions(context, manager!.Id, permissions, managerPerms);

        // User: can work on tasks/comments/attachments they own or are
        // assigned to (ownership enforced in the Service layer) - no
        // ManageAny, no project/team/user/role administration.
        string[] userPerms =
        {
            "Tasks.Update",
            "Tasks.Delete",

            "Comments.Create",
            "Comments.Update",
            "Comments.Delete",

            "Attachments.Create",
        };
        await SyncRolePermissions(context, user!.Id, permissions, userPerms);

        await context.SaveChangesAsync();
    }

    // FIX: previously this only ran once ever (guarded by "if any
    // RolePermission exists, return") and only ever added rows, so: (a) a
    // new permission added later never reached any role on an existing
    // database, and (b) there was no way to revoke a permission from a role
    // by editing this file - the stale grant would live in the DB forever.
    // This now reconciles each role's grants to exactly match the arrays
    // above: missing ones are added, no-longer-listed ones are removed.
    private static async Task SyncRolePermissions(
        AppDbContext context,
        string roleId,
        List<Permission> allPermissions,
        string[] allowedPermissionNames)
    {
        var desiredIds = allPermissions
            .Where(p => allowedPermissionNames.Contains(p.Name))
            .Select(p => p.Id)
            .ToHashSet();

        var current = await context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        var currentIds = current.Select(rp => rp.PermissionId).ToHashSet();

        var toRemove = current.Where(rp => !desiredIds.Contains(rp.PermissionId));
        context.RolePermissions.RemoveRange(toRemove);

        var toAdd = desiredIds
            .Where(id => !currentIds.Contains(id))
            .Select(id => new RolePermission { RoleId = roleId, PermissionId = id });
        await context.RolePermissions.AddRangeAsync(toAdd);
    }

    // =========================
    // ADMIN USER
    // =========================
    private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
    {
        const string email = "admin@taskmanager.com";

        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser != null)
            return;

        var admin = new ApplicationUser
        {
            UserName = "admin",
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(admin, "Admin123!");

        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}
