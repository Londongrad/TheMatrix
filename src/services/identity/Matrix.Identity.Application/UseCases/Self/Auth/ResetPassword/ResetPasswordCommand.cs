using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.ResetPassword
{
    public sealed record ResetPasswordCommand(
        Guid UserId,
        string Token,
        string NewPassword) : IRequest;
}
