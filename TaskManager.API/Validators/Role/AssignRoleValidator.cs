using FluentValidation;
using TaskManager.API.DTOs.User;

namespace TaskManager.API.Validators.Role
{
    public class AssignRoleValidator : AbstractValidator<AssignRoleDto>
    {
        public AssignRoleValidator()
        {
            RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

            RuleFor(x => x.RoleName)
                .NotEmpty().WithMessage("RoleName is required")
                .MaximumLength(50).WithMessage("RoleName must not exceed 50 characters");
        }
    }
}
