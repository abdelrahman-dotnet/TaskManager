using FluentValidation;
using TaskManager.API.DTOs.Invitation;

namespace TaskManager.API.Validators.Invitation
{
    public class InvitationCreateDtoValidator : AbstractValidator<InvitationCreateDto>
    {
        public InvitationCreateDtoValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .GreaterThan(0).WithMessage("WorkspaceId Is Required");

            RuleFor(x => x.InvitedUserId)
                .NotEmpty().WithMessage("InvitedUserId Is Required");

            RuleFor(x => x.ExpiresAt)
                .NotEmpty().WithMessage("ExpiresAt Is Required");
        }
    }

    public class InvitationResendDtoValidator : AbstractValidator<InvitationResendDto>
    {
        public InvitationResendDtoValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .GreaterThan(0).WithMessage("WorkspaceId Is Required");

            RuleFor(x => x.InvitedUserId)
                .NotEmpty().WithMessage("InvitedUserId Is Required");

            RuleFor(x => x.ExpiresAt)
                .NotEmpty().WithMessage("ExpiresAt Is Required");
        }
    }
}
