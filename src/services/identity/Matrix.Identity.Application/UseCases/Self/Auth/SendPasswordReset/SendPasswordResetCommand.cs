using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.SendPasswordReset
{
    public sealed record SendPasswordResetCommand(string Email) : IRequest;
}
