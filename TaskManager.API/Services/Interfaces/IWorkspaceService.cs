using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Services;
using TaskManager.API.DTOs.Workspace;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IWorkspaceService
    {
        Task<long> CreateWorkspaceAsync(WorkspaceCreateDto dto, string currentUserId, CancellationToken cancellationToken = default);

        Task<IEnumerable<WorkspaceReadDto>> GetMyWorkspacesAsync(string currentUserId, CancellationToken cancellationToken = default);
    }
}