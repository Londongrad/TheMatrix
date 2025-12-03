using MediatR;

namespace Matrix.Identity.Application.UseCases.Auth.RevokeRefreshToken
{
    public sealed record RevokeRefreshTokenCommand(string RefreshToken)
        : IRequest;
}
