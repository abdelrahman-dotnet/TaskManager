using System.ComponentModel.DataAnnotations;

namespace TaskManager.Data.Entities
{
    public class Team : BaseEntity
    {
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        // NOTE: not part of this round's Legacy-navigation list (TeamId/Team/Members) -
        // per earlier discussion this may eventually be superseded by a TeamMember with
        // Role = Owner, but that's a separate, later decision. Left untouched for now.
        public string ManagerId { get; set; } = null!;

        public ApplicationUser Manager { get; set; } = null!;

        // TODO:
        // Legacy navigation kept temporarily for incremental migration.
        // Remove after Membership system is fully adopted.
        // Do NOT use this in any new Service or Feature - use TeamMembers instead.
        public ICollection<ApplicationUser> Members { get; set; }
            = new List<ApplicationUser>();

        // NEW: the real Membership - many-to-many via TeamMember, each with their own Role.
        public ICollection<TeamMember> TeamMembers { get; set; }
            = new List<TeamMember>();

        public ICollection<Project> Projects { get; set; }
            = new List<Project>();
    }
}
