using FluentValidation;
using TaskManager.API.DTOs.Project;

public class ProjectUpdateDtoValidator : AbstractValidator<ProjectUpdateDto>
{
    public ProjectUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name Is Required")
            .MaximumLength(150).WithMessage("Maximum Length Is 150 Characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Maximum Length Is 2000 Characters");
    }
}