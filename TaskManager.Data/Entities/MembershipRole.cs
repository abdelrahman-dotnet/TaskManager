namespace TaskManager.Data.Entities
{
    // Shared by both TeamMember.Role and ProjectMember.Role - Team-level and Project-level
    // roles mean the same thing today (Owner/Manager/Member/Viewer), so one enum covers both
    // rather than two identical types. If Team and Project roles genuinely diverge later,
    // split this then - not before there's a real reason to.
    //
    // Business Authorization (who this person is *within a Team/Project*) - separate from
    // Identity Roles (ApplicationRole), which are System Authorization (Admin/Manager/User
    // and their Permissions).
    public enum MembershipRole
    {
        Owner = 1,
        Manager = 2,
        Member = 3,
        Viewer = 4
    }
}
