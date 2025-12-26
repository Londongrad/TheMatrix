using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class PermissionReadRepository(IdentityDbContext db)
        : IPermissionReadRepository
    {
        private readonly IdentityDbContext _db = db;

        public async Task<IReadOnlyCollection<PermissionCatalogItemResult>> GetPermissionsAsync(CancellationToken cancellationToken)
        {
            return await _db.Permissions
               .AsNoTracking()
               .OrderBy(p => p.Service)
               .ThenBy(p => p.Group)
               .ThenBy(p => p.Key)
               .Select(PermissionProjections.ToCatalogItem)
               .ToListAsync(cancellationToken);
        }

        public async Task<PermissionCatalogItemResult?> GetPermissionAsync(
            string permissionKey,
            CancellationToken cancellationToken)
        {
            return await _db.Permissions
               .AsNoTracking()
               .Where(p => p.Key == permissionKey)
               .Select(PermissionProjections.ToCatalogItem)
               .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
