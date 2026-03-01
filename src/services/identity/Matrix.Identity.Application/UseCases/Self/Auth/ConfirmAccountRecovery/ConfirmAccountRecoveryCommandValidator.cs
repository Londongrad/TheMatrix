using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery
{
    public sealed class ConfirmAccountRecoveryCommandValidator : AbstractValidator<ConfirmAccountRecoveryCommand>
    {
        public ConfirmAccountRecoveryCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.Token)
                .NotEmpty();
        }
    }
}
