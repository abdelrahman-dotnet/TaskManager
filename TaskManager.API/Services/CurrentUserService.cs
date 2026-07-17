using System.Security.Claims;
using TaskManager.API.Authorization;
using TaskManager.Business.Services.Interfaces;

namespace TaskManager.API.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal User =>
            _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        public bool IsAuthenticated =>
            User.Identity?.IsAuthenticated == true;

        public string UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User Id not found.");

        public string? UserName =>
            User.Identity?.Name;

        public IEnumerable<string> Permissions =>
            User.Claims
                .Where(x => x.Type == CustomClaimTypes.Permission)
                .Select(x => x.Value);

        public bool HasPermission(string permission)
        {
            return Permissions.Contains(permission);
        }
    }
}