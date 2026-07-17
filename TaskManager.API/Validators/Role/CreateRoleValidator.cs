using FluentValidation;
using TaskManager.API.DTOs.Role;

namespace TaskManager.API.Validators.Role
{
    public class CreateRoleValidator : AbstractValidator<RoleCreateAndUpdateDto>
    {
        public CreateRoleValidator()
        {
            RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("Description must not exceed 200 characters");
        }
    }
}
