using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.GetRolePermissions
{
    public sealed record GetRolePermissionsQuery(Guid RoleId)
        : IRequest<IReadOnlyCollection<string>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityRolePermissionsRead;
    }
}
