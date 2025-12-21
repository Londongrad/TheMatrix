using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ConfirmEmail
{
    public sealed record ConfirmEmailCommand(string Token) : IRequest;
}
