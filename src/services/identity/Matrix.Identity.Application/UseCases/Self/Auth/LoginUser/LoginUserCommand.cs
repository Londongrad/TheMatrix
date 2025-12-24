using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.LoginUser
{
    public sealed record LoginUserCommand(
        string Login,
        string Password,
        string DeviceId,
        string DeviceName,
        string UserAgent,
        string? IpAddress,
        bool RememberMe)
        : IRequest<LoginUserResult>;
}
