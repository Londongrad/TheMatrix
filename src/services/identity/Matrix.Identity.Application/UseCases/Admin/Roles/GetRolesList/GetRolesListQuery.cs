using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList
{
    public sealed record GetRolesListQuery : IRequest<IReadOnlyCollection<RoleListItemResult>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityRolesList;
    }
}
