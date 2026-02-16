using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail
{
    public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
    {
        public ConfirmEmailCommandValidator()
        {
            RuleFor(x => x.UserId)
               .NotEmpty();

            RuleFor(x => x.Token)
               .NotEmpty();
        }
    }
}
