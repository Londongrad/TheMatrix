using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile
{
    public sealed class ChangeAvatarFromFileCommandValidator : AbstractValidator<ChangeAvatarFromFileCommand>
    {
        public ChangeAvatarFromFileCommandValidator()
        {
            RuleFor(x => x.FileStream)
               .NotNull();

            RuleFor(x => x.FileName)
               .NotEmpty();

            RuleFor(x => x.ContentType)
               .NotEmpty();
        }
    }
}
