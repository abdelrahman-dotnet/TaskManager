using FluentValidation;
using TaskManager.API.DTOs.Task;
namespace TaskManager.API.Validators.Task

{
    public class ChangeTaskPriorityDtoValidator : AbstractValidator<ChangeTaskPriorityDto>
    {
        public ChangeTaskPriorityDtoValidator()
        {
            RuleFor(x => x.NewPriority)
                .IsInEnum();
        }
    }
}