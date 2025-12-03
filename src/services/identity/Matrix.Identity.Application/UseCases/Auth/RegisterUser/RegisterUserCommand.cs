using MediatR;

namespace Matrix.Identity.Application.UseCases.Auth.RegisterUser
{
    public sealed record RegisterUserCommand(
        string Email,
        string Username,
        string Password,
        string ConfirmPassword) : IRequest<RegisterUserResult>;
}
