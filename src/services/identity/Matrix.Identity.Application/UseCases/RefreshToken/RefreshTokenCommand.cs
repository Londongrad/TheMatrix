using Matrix.Identity.Application.UseCases.LoginUser;
using MediatR;

namespace Matrix.Identity.Application.UseCases.RefreshToken
{
    public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<LoginUserResult>;
}
