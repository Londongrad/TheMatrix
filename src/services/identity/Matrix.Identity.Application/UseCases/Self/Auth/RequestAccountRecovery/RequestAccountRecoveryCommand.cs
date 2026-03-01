using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RequestAccountRecovery
{
    public sealed record RequestAccountRecoveryCommand(
        string Email,
        string? IpAddress,
        string? UserAgent) : IRequest;
}
