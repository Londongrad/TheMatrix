using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.GetRoleMembersPage
{
    public sealed record GetRoleMembersPageQuery(
        Guid RoleId,
        Pagination Pagination)
        : IRequest<PagedResult<UserListItemResult>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityRoleMembersRead;
    }
}
