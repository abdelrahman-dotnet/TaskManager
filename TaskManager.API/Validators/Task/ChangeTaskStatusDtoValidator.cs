using FluentValidation;
using TaskManager.API.DTOs.Task;

namespace TaskManager.API.Validators.Task
{
    public class ChangeTaskItemStatusDtoValidator : AbstractValidator<ChangeTaskItemStatusDto>
    {
        public ChangeTaskItemStatusDtoValidator()
        {
            RuleFor(x => x.NewStatus)
                .IsInEnum();
        }
    }
}
