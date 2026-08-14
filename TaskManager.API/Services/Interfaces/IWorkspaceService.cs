using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Services;
using TaskManager.API.DTOs.Workspace;

namespace TaskManager.Business.Services.Interfaces
{
    public interface IWorkspaceService
    {
        Task<long> CreateWorkspaceAsync(WorkspaceCreateDto dto, string currentUserId, CancellationToken cancellationToken = default);

        Task<IEnumerable<WorkspaceReadDto>> GetMyWorkspacesAsync(string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE: Visibility -> Permission (Workspace.Update) -> BR-WS-02 (guard).
        Task SuspendWorkspaceAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE: Visibility -> Permission (Workspace.Update) -> BR-WS-02 (guard).
        Task ActivateWorkspaceAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE: Visibility -> Permission (Workspace.Archive) -> BR-WS-03 (guard).
        Task ArchiveWorkspaceAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE: Visibility -> Permission (Workspace.Update) -> BR-WS-03 (guard).
        Task RestoreWorkspaceAsync(long workspaceId, string currentUserId, CancellationToken cancellationToken = default);

        // PIPELINE: Visibility -> Permission (Workspace.TransferOwnership) -> BR-WS-04.
        Task TransferOwnershipAsync(long workspaceId, string targetUserId, string currentUserId, CancellationToken cancellationToken = default);
    }
}