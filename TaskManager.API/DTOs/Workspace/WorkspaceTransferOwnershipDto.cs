using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.Workspace
{
    public class WorkspaceTransferOwnershipDto
    {
        [Required]
        public string TargetUserId { get; set; } = null!;
    }
}
