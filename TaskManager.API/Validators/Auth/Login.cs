using FluentValidation;
using TaskManager.API.DTOs.Account;

namespace TaskManager.API.Validators.Auth
{
    public class Login : AbstractValidator<LoginDto>
    {
        public Login()
        {
            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress();
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
}
