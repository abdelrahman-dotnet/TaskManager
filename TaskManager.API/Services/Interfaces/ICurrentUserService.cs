using TaskManager.Data.Entities;

namespace TaskManager.Business.Services.Interfaces
{
    public interface ICurrentUserService
    {
        string UserId { get; }

        string? UserName { get; }

        bool IsAuthenticated { get; }

        bool HasPermission(string permission);

        IEnumerable<string> Permissions { get; }
    }
}