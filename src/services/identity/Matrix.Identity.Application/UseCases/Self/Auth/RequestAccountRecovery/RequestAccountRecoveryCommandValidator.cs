using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RequestAccountRecovery
{
    public sealed class RequestAccountRecoveryCommandValidator : AbstractValidator<RequestAccountRecoveryCommand>
    {
        public RequestAccountRecoveryCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Email format is invalid.");
        }
    }
}
