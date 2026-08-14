using TaskManager.API.DTOs.AuditLog;
using TaskManager.API.DTOs.FilterQueryParams;
using TaskManager.API.Helpers;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task<PagedResult<AuditLogReadDto>> GetAllAsync(AuditLogQueryParams queryParams, string currentUserId, CancellationToken cancellationToken = default);

        // cancellationToken is optional with a default value, so every existing call site
        // (ProjectService, RoleService, ...) that doesn't pass it still compiles unchanged.
        // workspaceId is required so every audit row belongs to the correct workspace scope;
        // if null, the row is written but remains un-scoped (platform-level actions).
        Task LogAsync(string? userId, string action, string entityName, string entityId, long? workspaceId = null, string? oldValues = null, string? newValues = null, CancellationToken cancellationToken = default);
    }
}
