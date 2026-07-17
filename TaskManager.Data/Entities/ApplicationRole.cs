using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Data.Entities
{
    public class ApplicationRole : IdentityRole
    {

        [MaxLength(200)]
        public string? Description { get; set; }
            public ICollection<RolePermission> RolePermissions { get; set; }
                = new List<RolePermission>();
        
    }
}
