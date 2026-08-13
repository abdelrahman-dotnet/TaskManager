using AutoMapper;
using TaskManager.API.DTOs.TaskItemStatusHistory;
using TaskManager.Data.Entities;

namespace TaskManager.API.Mapping
{
    public class TaskItemStatusHistoryProfile : Profile
    {
        public TaskItemStatusHistoryProfile()
        {
            CreateMap<TaskItemStatusHistory, TaskItemStatusHistoryReadDto>();
        }
    }
}
