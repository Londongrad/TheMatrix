using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken
{
    public sealed record RevokeRefreshTokenCommand(string RefreshToken)
        : IRequest;
}
