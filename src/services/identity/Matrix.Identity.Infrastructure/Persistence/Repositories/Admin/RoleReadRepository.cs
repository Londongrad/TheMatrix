using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class RoleReadRepository(IdentityDbContext db)
        : IRoleReadRepository
    {
        private readonly IdentityDbContext _db = db;

        public async Task<IReadOnlyCollection<RoleListItemResult>> GetRolesAsync(CancellationToken cancellationToken)
        {
            return await _db.Roles
               .AsNoTracking()
               .OrderBy(r => r.Name)
               .Select(r => new RoleListItemResult
                {
                    Id = r.Id,
                    Name = r.Name,
                    IsSystem = r.IsSystem,
                    CreatedAtUtc = r.CreatedAtUtc
                })
               .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<Guid>> GetExistingRoleIdsAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken)
        {
            if (roleIds.Count == 0)
                return Array.Empty<Guid>();

            return await _db.Roles
               .AsNoTracking()
               .Where(role => roleIds.Contains(role.Id))
               .Select(role => role.Id)
               .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            return await _db.Roles
               .AsNoTracking()
               .AnyAsync(
                    predicate: r => r.Id == roleId,
                    cancellationToken: cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(
            string roleName,
            CancellationToken cancellationToken)
        {
            string normalizedName = roleName.Trim()
               .ToUpperInvariant();

            return await _db.Roles
               .AsNoTracking()
               .AnyAsync(
                    predicate: r => r.NormalizedName == normalizedName,
                    cancellationToken: cancellationToken);
        }

        public async Task<bool> ExistsByNameExceptAsync(
            string roleName,
            Guid excludedRoleId,
            CancellationToken cancellationToken)
        {
            string normalizedName = roleName.Trim()
               .ToUpperInvariant();

            return await _db.Roles
               .AsNoTracking()
               .AnyAsync(
                    predicate: r => r.NormalizedName == normalizedName && r.Id != excludedRoleId,
                    cancellationToken: cancellationToken);
        }

        public async Task<Role?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _db.Roles
               .AsNoTracking()
               .FirstOrDefaultAsync(
                    predicate: r => r.Id == id,
                    cancellationToken: cancellationToken);
        }
    }
}
