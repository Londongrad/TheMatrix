using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.SendEmailConfirmation
{
    public sealed record SendEmailConfirmationCommand(string Email) : IRequest;
}
