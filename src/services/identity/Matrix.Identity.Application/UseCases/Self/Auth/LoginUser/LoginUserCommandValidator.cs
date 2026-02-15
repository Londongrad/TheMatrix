using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Auth.LoginUser
{
    public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserCommandValidator()
        {
            RuleFor(x => x.Login)
               .NotEmpty();

            RuleFor(x => x.Password)
               .NotEmpty();

            RuleFor(x => x.DeviceId)
               .NotEmpty();

            RuleFor(x => x.DeviceName)
               .NotEmpty();

            RuleFor(x => x.UserAgent)
               .NotEmpty();
        }
    }
}
