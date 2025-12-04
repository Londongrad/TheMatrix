using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ChangePassword
{
    public sealed record ChangePasswordCommand(
        Guid UserId,
        string CurrentPassword,
        string NewPassword,
        string ConfirmPassword
    ) : IRequest;
}
