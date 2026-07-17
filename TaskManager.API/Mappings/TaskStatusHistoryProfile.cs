using AutoMapper;
using TaskManager.API.DTOs.TaskStatusHistory;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class TaskStatusHistoryProfile : Profile
    {
        public TaskStatusHistoryProfile()
        {
            CreateMap<TaskStatusHistory, TaskStatusHistoryReadDto>();
        }
    }
}
