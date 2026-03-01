using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.CancelPendingEmailChange
{
    public sealed record CancelPendingEmailChangeCommand(
        string? IpAddress,
        string? UserAgent) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeEmailChange;
    }
}
