using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TaskManager.Data.Entities;

namespace TaskManager.Data.Seeders
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

            // 1. Seed Roles
            string[] roles = { "Manager", "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new Role { Name = role });
                }
            }
            
            // 2. Seed Admin User
            var adminEmail = "abdo@task.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                // لازم باسورد قوي
                var result = await userManager.CreateAsync(admin, "StrongPassword123!");
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        Console.WriteLine(error.Description);
                    return; // وقف لو فيه مشكلة
                }

                // أعد تحميل المستخدم من الداتا
                admin = await userManager.FindByEmailAsync(adminEmail);
            }

            // 3. Assign Role
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}