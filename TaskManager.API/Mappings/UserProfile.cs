using AutoMapper;
using TaskManager.API.DTOs.User;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<ApplicationUser, UserReadDto>()
                .ForMember(d => d.TeamIds, o => o.MapFrom(s =>
                    s.WorkspaceMemberships.SelectMany(wm => wm.TeamMemberships).Select(tm => tm.TeamId)))
                .ForMember(d => d.TeamNames, o => o.MapFrom(s =>
                    s.WorkspaceMemberships.SelectMany(wm => wm.TeamMemberships).Select(tm => tm.Team.Name)))
                .ForMember(d => d.ProjectIds, o => o.MapFrom(s =>
                    s.WorkspaceMemberships.SelectMany(wm => wm.ProjectMemberships).Select(pm => pm.ProjectId)))
                .ForMember(d => d.ProjectNames, o => o.MapFrom(s =>
                    s.WorkspaceMemberships.SelectMany(wm => wm.ProjectMemberships).Select(pm => pm.Project.Name)))
                .ForMember(d => d.Roles, o => o.Ignore()); // filled in separately via UserManager.GetRolesAsync
        }
    }
}