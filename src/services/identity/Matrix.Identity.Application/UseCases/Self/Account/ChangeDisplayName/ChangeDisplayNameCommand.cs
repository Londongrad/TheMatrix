using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName
{
    public sealed record ChangeDisplayNameCommand(
        string? DisplayName) : IRequest<string?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeDisplayNameChange;
    }
}
