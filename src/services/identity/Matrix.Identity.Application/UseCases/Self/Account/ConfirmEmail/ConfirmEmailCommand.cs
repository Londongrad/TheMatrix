using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail
{
    public sealed record ConfirmEmailCommand(Guid UserId, string Token) : IRequest;
}
