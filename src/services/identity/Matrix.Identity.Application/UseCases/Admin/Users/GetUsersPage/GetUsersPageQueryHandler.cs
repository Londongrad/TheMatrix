using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage
{
    public sealed class GetUsersPageQueryHandler(IUserAdminReadRepository repo)
        : IRequestHandler<GetUsersPageQuery, PagedResult<UserListItemResult>>
    {
        public Task<PagedResult<UserListItemResult>> Handle(
            GetUsersPageQuery request,
            CancellationToken cancellationToken)
        {
            return repo.GetUsersPageAsync(
                pagination: request.Pagination,
                cancellationToken: cancellationToken);
        }
    }
}
