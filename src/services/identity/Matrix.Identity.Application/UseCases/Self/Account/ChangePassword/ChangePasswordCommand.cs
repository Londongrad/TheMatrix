using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangePassword
{
    public sealed record ChangePasswordCommand(
        string CurrentPassword,
        string NewPassword) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMePasswordChange;
    }
}
