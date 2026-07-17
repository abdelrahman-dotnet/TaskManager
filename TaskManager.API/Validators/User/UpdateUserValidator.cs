using FluentValidation;
using TaskManager.API.DTOs.User;

namespace TaskManager.API.Validators.User
{
    public class UpdateUserValidator : AbstractValidator<UserUpdateDto>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("User Name Is Required")
                .MaximumLength(10).WithMessage("User Name must Not Exceed 10 Characters");
        }
    }
}