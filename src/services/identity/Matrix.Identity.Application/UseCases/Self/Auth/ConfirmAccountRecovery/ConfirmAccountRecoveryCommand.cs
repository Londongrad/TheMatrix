using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery
{
    public sealed record ConfirmAccountRecoveryCommand(
        Guid UserId,
        string Token,
        string? IpAddress,
        string? UserAgent) : IRequest;
}
