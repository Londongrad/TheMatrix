using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IUserAdminReadRepository
    {
        Task<PagedResult<UserListItemResult>> GetUsersPageAsync(
            Pagination pagination,
            CancellationToken cancellationToken);
    }
}
