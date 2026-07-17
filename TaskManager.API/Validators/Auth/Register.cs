using FluentValidation;
using TaskManager.API.DTOs.Account;

namespace TaskManager.API.Validators.Auth
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(100).WithMessage("Username must not exceed 100 characters");
            //RuleFor(x => x.LastName)
            //.NotEmpty().WithMessage("Username is required")
            //.MinimumLength(3).WithMessage("Username must be at least 3 characters")
            //.MaximumLength(100).WithMessage("Username must not exceed 100 characters");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(200).WithMessage("Email must not exceed 200 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#]).+$")
                .WithMessage("Password must contain uppercase, lowercase, number, and special character");
        }
    }
}
