using MediatR;

namespace Matrix.Identity.Application.UseCases.RevokeRefreshToken
{
    public sealed record RevokeRefreshTokenCommand(string RefreshToken)
        : IRequest;
}
