using AutoMapper;
using TaskManager.API.DTOs.Task;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class TaskProfile : Profile
    {
        public TaskProfile()
        {
            CreateMap<TaskItem, TaskReadDto>()
                .ForMember(d => d.ProjectName, o => o.MapFrom(s => s.Project != null ? s.Project.Name : null));

            CreateMap<TaskItem, TaskDetailsReadDto>()
                .ForMember(d => d.ProjectName, o => o.MapFrom(s => s.Project != null ? s.Project.Name : null))
                .ForMember(d => d.CommentsCount, o => o.MapFrom(s => s.Comments.Count))
                .ForMember(d => d.AttachmentsCount, o => o.MapFrom(s => s.Attachments.Count))
                .ForMember(d => d.Assignments, o => o.MapFrom(s => s.Assignments));

            CreateMap<TaskAssignment, TaskAssignmentMiniDto>()
                .ForMember(d => d.UserFullName, o => o.MapFrom(s => s.User != null ? s.User.UserName : null));

            CreateMap<TaskCreateDto, TaskItem>();
            CreateMap<TaskUpdateDto, TaskItem>();
        }
    }
}