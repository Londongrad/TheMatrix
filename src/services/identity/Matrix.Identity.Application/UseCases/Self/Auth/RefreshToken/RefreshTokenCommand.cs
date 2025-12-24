using Matrix.Identity.Application.UseCases.Self.Auth.LoginUser;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken
{
    public sealed record RefreshTokenCommand(
        string RefreshToken,
        string DeviceId,
        string UserAgent,
        string? IpAddress) : IRequest<LoginUserResult>;
}
