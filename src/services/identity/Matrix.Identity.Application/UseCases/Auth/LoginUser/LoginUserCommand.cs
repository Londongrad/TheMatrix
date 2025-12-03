using MediatR;

namespace Matrix.Identity.Application.UseCases.Auth.LoginUser
{
    public sealed record LoginUserCommand(
        string Login,
        string Password,
        string DeviceId,
        string DeviceName,
        string UserAgent,
        string? IpAddress)
        : IRequest<LoginUserResult>;
}
