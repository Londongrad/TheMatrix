using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Domain.Entities;
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
               .OrderByDescending(x => x.CreatedAtUtc);

            int totalCount = await query.CountAsync(cancellationToken);

            List<UserListItemResult> items = await query
               .Skip(pagination.Skip)
               .Take(pagination.PageSize)
               .Select(u => new UserListItemResult
                {
                    Id = u.Id,
                    AvatarUrl = u.AvatarUrl,
                    Email = u.Email.Value,
                    Username = u.Username.Value,
                    IsEmailConfirmed = u.IsEmailConfirmed,
                    IsLocked = u.IsLocked,
                    CreatedAtUtc = u.CreatedAtUtc
                })
               .ToListAsync(cancellationToken);

            return new PagedResult<UserListItemResult>(
                items: items,
                totalCount: totalCount,
                pageNumber: pagination.PageNumber,
                pageSize: pagination.PageSize);
        }
    }
}
