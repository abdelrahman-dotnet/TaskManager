using FluentValidation;
using TaskManager.API.DTOs.Task;

namespace TaskManager.API.Validators.Task
{
    public class ChangeTaskStatusDtoValidator : AbstractValidator<ChangeTaskStatusDto>
    {
        public ChangeTaskStatusDtoValidator()
        {
            RuleFor(x => x.NewStatus)
                .IsInEnum();
        }
    }
}