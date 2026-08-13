using AutoMapper;
using TaskManager.API.DTOs.TaskAssignment;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class TaskAssignmentProfile : Profile
    {
        public TaskAssignmentProfile()
        {
            CreateMap<TaskAssignment, TaskAssignmentReadDto>()
                .ForMember(d => d.UserFullName, o => o.MapFrom(s => s.WorkspaceMember != null && s.WorkspaceMember.User != null ? s.WorkspaceMember.User.UserName : null));
        }
    }
}
