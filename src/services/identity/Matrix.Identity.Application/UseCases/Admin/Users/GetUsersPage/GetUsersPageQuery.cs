using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.BuildingBlocks.Application.Models;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage
{
    public sealed record GetUsersPageQuery(Pagination Pagination)
        : IRequest<PagedResult<UserListItemResult>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUsersRead;
    }
}
