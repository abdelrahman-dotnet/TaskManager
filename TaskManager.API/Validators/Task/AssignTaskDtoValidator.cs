using FluentValidation;
using TaskManager.API.DTOs.Task;

namespace TaskManager.API.Validators.Task
{
    public class AssignTaskDtoValidator : AbstractValidator<AssignTaskDto>
    {
        public AssignTaskDtoValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();
        }
    }
}