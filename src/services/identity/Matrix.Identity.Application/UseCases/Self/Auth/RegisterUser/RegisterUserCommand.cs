using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RegisterUser
{
    public sealed record RegisterUserCommand(
        string Email,
        string Username,
        string Password) : IRequest<RegisterUserResult>;
}
