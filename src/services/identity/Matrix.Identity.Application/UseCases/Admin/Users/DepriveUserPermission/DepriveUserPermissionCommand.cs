using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.DepriveUserPermission
{
    public sealed record DepriveUserPermissionCommand(
        Guid UserId,
        string TargetPermissionKey)
        : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUserPermissionsDeprive;
    }
}
