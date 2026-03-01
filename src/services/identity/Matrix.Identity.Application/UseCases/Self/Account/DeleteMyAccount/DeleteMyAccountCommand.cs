using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount
{
    public sealed record DeleteMyAccountCommand(
        string CurrentPassword,
        string? IpAddress,
        string? UserAgent) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeAccountDelete;
    }
}
