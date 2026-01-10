using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage
{
    public sealed record GetUsersPageQuery(Pagination Pagination)
        : IRequest<PagedResult<UserListItemResult>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUsersRead;
    }
}
