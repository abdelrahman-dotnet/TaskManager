using FluentValidation;
using TaskManager.API.DTOs.Task;

namespace TaskManager.API.Validators.Task
{
    public class UpdateTaskValidator : AbstractValidator<TaskUpdateDto>
    {
        public UpdateTaskValidator()
        {
            RuleFor(x => x.Title)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Title Is Required")
                .MinimumLength(3);

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Maximum Length Is 1000 Characters");

            RuleFor(x => x.ProjectId)
                .GreaterThan(0).WithMessage("ProjectId is required");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Due Date Must Be In The Fiture")
                .When(x => x.DueDate.HasValue);
        }
    }
}