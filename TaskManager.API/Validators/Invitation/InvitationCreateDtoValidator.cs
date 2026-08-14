using FluentValidation;
using TaskManager.API.DTOs.Invitation;

namespace TaskManager.API.Validators.Invitation
{
    public class InvitationCreateDtoValidator : AbstractValidator<InvitationCreateDto>
    {
        public InvitationCreateDtoValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .GreaterThan(0).WithMessage("WorkspaceId must be a positive identifier.");

            RuleFor(x => x.InvitedUserId)
                .NotEmpty().WithMessage("InvitedUserId is required.");

            RuleFor(x => x.ExpiresAt)
                .NotEmpty().WithMessage("ExpiresAt is required.")
                .GreaterThan(DateTime.UtcNow.AddMinutes(-1)).WithMessage("The invitation expiry must be in the future.");
        }
    }

    public class InvitationResendDtoValidator : AbstractValidator<InvitationResendDto>
    {
        public InvitationResendDtoValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .GreaterThan(0).WithMessage("WorkspaceId must be a positive identifier.");

            RuleFor(x => x.InvitedUserId)
                .NotEmpty().WithMessage("InvitedUserId is required.");

            RuleFor(x => x.ExpiresAt)
                .NotEmpty().WithMessage("ExpiresAt is required.")
                .GreaterThan(DateTime.UtcNow.AddMinutes(-1)).WithMessage("The invitation expiry must be in the future.");
        }
    }
}
