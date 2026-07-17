using FluentValidation;
using TaskManager.API.DTOs.Project;

namespace TaskManager.API.Validators.Project
{
    public class ProjectCreateDtoValidator : AbstractValidator<ProjectCreateDto>
    {
        public ProjectCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name Is Required")
                .MaximumLength(150).WithMessage("Maximum Length Is 150 Characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Maximum Length Is 2000 Characters");

            RuleFor(x => x.TeamId)
                .GreaterThan(0).WithMessage("Invalid Team Id");
        }
    }
}
