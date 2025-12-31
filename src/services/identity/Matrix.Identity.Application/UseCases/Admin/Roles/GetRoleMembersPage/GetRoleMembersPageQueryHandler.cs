using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.GetRoleMembersPage
{
    public sealed class GetRoleMembersPageQueryHandler(
        IRoleMembersReadRepository membersReadRepository,
        IRoleReadRepository roleReadRepository)
        : IRequestHandler<GetRoleMembersPageQuery, PagedResult<UserListItemResult>>
    {
        public async Task<PagedResult<UserListItemResult>> Handle(
            GetRoleMembersPageQuery request,
            CancellationToken cancellationToken)
        {
            bool isExist = await roleReadRepository.ExistsAsync(
                request.RoleId,
                cancellationToken);

            if (!isExist)
                throw ApplicationErrorsFactory.RoleNotFound(request.RoleId);

            return await membersReadRepository.GetRoleMembersPageAsync(
                roleId: request.RoleId,
                pagination: request.Pagination,
                cancellationToken: cancellationToken);
        }
    }
}
