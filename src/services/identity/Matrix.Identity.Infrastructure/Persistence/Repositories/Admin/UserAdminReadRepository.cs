using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class UserAdminReadRepository(IdentityDbContext dbContext)
        : IUserAdminReadRepository, IRoleMembersReadRepository
    {
        public async Task<PagedResult<UserListItemResult>> GetRoleMembersPageAsync(
            Guid roleId,
            Pagination pagination,
            CancellationToken cancellationToken)
        {
            IQueryable<User> query = from user in dbContext.Users.AsNoTracking()
                                     join ur in dbContext.UserRoles.AsNoTracking()
                                         on user.Id equals ur.UserId
                                     where ur.RoleId == roleId
                                     orderby user.CreatedAtUtc descending, user.Id
                                     select user;

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
