using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.DeleteRole
{
    public sealed record DeleteRoleCommand(Guid RoleId) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityRolesDelete;
    }
}
