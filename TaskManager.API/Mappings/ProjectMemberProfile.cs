using AutoMapper;
using TaskManager.API.DTOs.ProjectMember;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class ProjectMemberProfile : Profile
    {
        public ProjectMemberProfile()
        {
            CreateMap<ProjectMember, ProjectMemberReadDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.WorkspaceMember != null && s.WorkspaceMember.User != null ? s.WorkspaceMember.User.UserName : null));

            // Convenience map for the Service - ProjectId/Id/CreatedAt are still set explicitly
            // there (ProjectId comes from the route, not the DTO).
            CreateMap<AddProjectMemberDto, ProjectMember>();
        }
    }
}
