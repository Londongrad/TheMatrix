using Matrix.Identity.Application.UseCases.Auth.LoginUser;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Auth.RefreshToken
{
    public sealed record RefreshTokenCommand(
        string RefreshToken,
        string DeviceId,
        string UserAgent,
        string? IpAddress) : IRequest<LoginUserResult>;
}
