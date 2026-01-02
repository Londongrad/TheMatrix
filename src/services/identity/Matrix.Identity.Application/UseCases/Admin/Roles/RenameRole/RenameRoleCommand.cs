using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.RenameRole
{
    public sealed record RenameRoleCommand(Guid RoleId, string Name)
        : IRequest<RoleRenamedResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityRolesRename;
    }
}
