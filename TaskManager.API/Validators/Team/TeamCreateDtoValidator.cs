using FluentValidation;
using TaskManager.API.DTOs.Team;

namespace TaskManager.API.Validators.Team
{
    public class TeamCreateDtoValidator : AbstractValidator<TeamCreateDto>
    {
        public TeamCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name Is Required")
                .MaximumLength(100).WithMessage("Maximum Length Is 100 Characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Maximum Length Is 1000 Characters");
        }

    }
}
