using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.Identity.Application.Authorization.Permissions.PermissionKeys;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserPermissions
{
    public sealed record UpdateUserPermissionOverrideInput(
        string PermissionKey,
        string Effect);

    public sealed record UpdateUserPermissionsCommand(
        Guid UserId,
        IReadOnlyCollection<UpdateUserPermissionOverrideInput> Overrides)
        : IRequest, IRequirePermissions
    {
        public IReadOnlyCollection<string> PermissionKeys =>
        [
            AppPermissionKeys.IdentityUserPermissionsGrant,
            AppPermissionKeys.IdentityUserPermissionsDeprive
        ];

        public PermissionMatchMode PermissionMatchMode => PermissionMatchMode.All;
    }
}
