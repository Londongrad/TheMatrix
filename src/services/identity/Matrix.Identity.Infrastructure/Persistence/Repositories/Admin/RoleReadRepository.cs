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

        public async Task<IReadOnlyCollection<RoleListItemResult>> GetRolesAsync(CancellationToken ct)
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
               .ToListAsync(ct);
        }

        public async Task<IReadOnlyCollection<Guid>> GetExistingRoleIdsAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken ct)
        {
            if (roleIds.Count == 0)
                return Array.Empty<Guid>();

            return await _db.Roles
               .AsNoTracking()
               .Where(role => roleIds.Contains(role.Id))
               .Select(role => role.Id)
               .ToListAsync(ct);
        }
    }
}
