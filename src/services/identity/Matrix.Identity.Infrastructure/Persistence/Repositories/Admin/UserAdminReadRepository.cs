using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class UserAdminReadRepository(IdentityDbContext dbContext) : IUserAdminReadRepository
    {
        public async Task<PagedResult<UserListItemResult>> GetPageAsync(
            Pagination pagination,
            CancellationToken cancellationToken)
        {
            IQueryable<User> query = dbContext.Users
               .AsNoTracking()
               .OrderByDescending(x => x.CreatedAtUtc)
               .ThenBy(x => x.Id);

            int totalCount = await query.CountAsync(cancellationToken);

            List<UserListItemResult> items = await query
               .Skip(pagination.Skip)
               .Take(pagination.PageSize)
               .Select(UserProjections.ToListItem)
               .ToListAsync(cancellationToken);

            return new PagedResult<UserListItemResult>(
                items: items,
                totalCount: totalCount,
                pageNumber: pagination.PageNumber,
                pageSize: pagination.PageSize);
        }
    }
}
