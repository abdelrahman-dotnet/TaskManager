using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Services
{
    public interface ITokenService
    {
        // FIX: was synchronous ("GenerateToken") while its implementation ran
        // a DB query internally (_context.Roles...ToList()) -> blocking the
        // thread pool. Made properly async end-to-end.
        Task<string> GenerateTokenAsync(ApplicationUser user, IList<string> roles);
    }
}
