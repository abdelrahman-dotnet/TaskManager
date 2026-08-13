using FluentValidation;
using TaskManager.API.DTOs.Workspace;

namespace TaskManager.API.Validators.Workspace
{
    public class WorkspaceCreateDtoValidator : AbstractValidator<WorkspaceCreateDto>
    {
        public WorkspaceCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name Is Required")
                .MaximumLength(150).WithMessage("Maximum Length Is 150 Characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Maximum Length Is 2000 Characters");
        }
    }
}