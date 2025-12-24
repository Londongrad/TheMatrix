using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class UserAdminReadRepository(IdentityDbContext db)
        : IUserAdminReadRepository
    {
        private readonly IdentityDbContext _db = db;

        public async Task<PagedResult<UserListItemResult>> GetUsersPageAsync(
            Pagination pagination,
            CancellationToken cancellationToken)
        {
            // В реальных системах всегда есть paging + AsNoTracking + projection.
            IQueryable<User> query = _db.Users
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
