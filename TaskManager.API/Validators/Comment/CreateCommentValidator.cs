using FluentValidation;
using TaskManager.API.DTOs.Comment;

namespace TaskManager.API.Validators.Comment
{
    public class CreateCommentValidator : AbstractValidator<CommentCreateDto>
    {
        public CreateCommentValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content Is Required")
                .MaximumLength(1000).WithMessage("Content must not exceed 1000 characters");
        }
    }
}