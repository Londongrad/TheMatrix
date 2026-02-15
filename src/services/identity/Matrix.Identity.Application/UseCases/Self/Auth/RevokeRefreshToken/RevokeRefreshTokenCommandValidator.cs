using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken
{
    public sealed class RevokeRefreshTokenCommandValidator : AbstractValidator<RevokeRefreshTokenCommand>
    {
        public RevokeRefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
               .NotEmpty();
        }
    }
}
