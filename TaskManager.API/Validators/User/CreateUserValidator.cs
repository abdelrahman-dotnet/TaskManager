using FluentValidation;
using TaskManager.API.DTOs.User;

namespace TaskManager.API.Validators.User
{
    public class CreateUserValidator : AbstractValidator<UserCreateDto>
    {
        public CreateUserValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("User Name Is Required")
                .MaximumLength(10).WithMessage("User Name must Not Exceed 10 Characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email Is Required")
                .EmailAddress().WithMessage("Invalid Email Format")
                .MaximumLength(200).WithMessage("Email Must Not Exceed 200 Charachters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#]).+$")
                .WithMessage("Password must contain uppercase, lowercase, number, and special character");
        }
    }
}