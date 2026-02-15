using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession
{
    public sealed class RevokeMySessionCommandValidator : AbstractValidator<RevokeMySessionCommand>
    {
        public RevokeMySessionCommandValidator()
        {
            RuleFor(x => x.SessionId)
               .NotEmpty();
        }
    }
}
