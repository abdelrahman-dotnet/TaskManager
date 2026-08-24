using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManager.Data.Context
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Design-time tooling must never silently target an unrelated database.
            // Supply TASKMANAGER_DESIGNTIME_CONNECTION for EF commands, or use the
            // standard .NET configuration environment-variable convention.
            var connectionString =
                Environment.GetEnvironmentVariable("TASKMANAGER_DESIGNTIME_CONNECTION") ??
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "A design-time database target is required. Set TASKMANAGER_DESIGNTIME_CONNECTION " +
                    "(preferred) or ConnectionStrings__DefaultConnection before running EF Core tooling.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
