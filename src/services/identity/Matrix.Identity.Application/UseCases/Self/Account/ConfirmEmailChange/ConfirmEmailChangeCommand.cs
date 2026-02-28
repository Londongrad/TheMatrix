using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange
{
    public sealed record ConfirmEmailChangeCommand(
        Guid UserId,
        string Token,
        string? IpAddress,
        string? UserAgent) : IRequest;
}
