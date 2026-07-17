using FluentValidation;
using TaskManager.API.DTOs.Task;
namespace TaskManager.API.Validators.Task
{
    public class CreateTaskValidator : AbstractValidator<TaskCreateDto>
    {
        public CreateTaskValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MinimumLength(3).WithMessage("Title must be at least 3 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500);

            RuleFor(x => x.ProjectId)
                .GreaterThan(0).WithMessage("ProjectId is required");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Due date must be in the future")
                .When(x => x.DueDate.HasValue);

            RuleFor(x => x.Priority)
                .IsInEnum();
        }
    }
}