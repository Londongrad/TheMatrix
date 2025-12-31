using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class PermissionReadRepository(IdentityDbContext db)
        : IPermissionReadRepository
    {
        private readonly IdentityDbContext _db = db;

        public async Task<IReadOnlyCollection<PermissionCatalogItemResult>> GetPermissionsAsync(CancellationToken ct)
        {
            return await _db.Permissions
               .AsNoTracking()
               .OrderBy(p => p.Service)
               .ThenBy(p => p.Group)
               .ThenBy(p => p.Key)
               .Select(p => new PermissionCatalogItemResult
                {
                    Key = p.Key,
                    Service = p.Service,
                    Group = p.Group,
                    Description = p.Description,
                    IsDeprecated = p.IsDeprecated
                })
               .ToListAsync(ct);
        }

        public Task<PermissionCatalogItemResult?> GetPermissionAsync(
            string permissionKey,
            CancellationToken ct)
        {
            return _db.Permissions
               .AsNoTracking()
               .Where(p => p.Key == permissionKey)
               .Select(p => new PermissionCatalogItemResult
                {
                    Key = p.Key,
                    Service = p.Service,
                    Group = p.Group,
                    Description = p.Description,
                    IsDeprecated = p.IsDeprecated
                })
               .FirstOrDefaultAsync(ct);
        }
    }
}
