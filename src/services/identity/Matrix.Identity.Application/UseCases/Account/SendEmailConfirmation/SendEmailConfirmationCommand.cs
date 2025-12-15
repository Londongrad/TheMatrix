using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.SendEmailConfirmation
{
    public sealed record SendEmailConfirmationCommand(string Email) : IRequest;
}
