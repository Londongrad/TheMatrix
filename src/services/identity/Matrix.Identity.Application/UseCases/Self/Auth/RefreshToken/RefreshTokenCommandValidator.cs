using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken
{
    public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
               .NotEmpty();

            RuleFor(x => x.DeviceId)
               .NotEmpty();

            RuleFor(x => x.UserAgent)
               .NotEmpty();
        }
    }
}
