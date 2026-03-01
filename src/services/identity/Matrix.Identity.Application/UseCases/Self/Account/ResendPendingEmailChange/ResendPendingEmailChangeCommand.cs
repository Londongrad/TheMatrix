using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ResendPendingEmailChange
{
    public sealed record ResendPendingEmailChangeCommand(
        string? IpAddress,
        string? UserAgent) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeEmailChange;
    }
}
