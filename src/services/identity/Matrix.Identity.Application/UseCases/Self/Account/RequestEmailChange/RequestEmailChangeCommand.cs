using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange
{
    public sealed record RequestEmailChangeCommand(
        string NewEmail,
        string CurrentPassword,
        string? IpAddress,
        string? UserAgent) : IRequest<string>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeEmailChange;
    }
}
