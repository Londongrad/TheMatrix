using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ChangePassword
{
    public sealed record ChangePasswordCommand(
        string CurrentPassword,
        string NewPassword) : IRequest;
}
