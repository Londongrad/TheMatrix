using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class RolePermissionsRepository(IdentityDbContext db) : IRolePermissionsRepository
    {
        private readonly IdentityDbContext _db = db;

        public async Task<IReadOnlyCollection<string>> GetRolePermissionsAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            return await _db.RolePermissions
               .AsNoTracking()
               .Where(rp => rp.RoleId == roleId)
               .OrderBy(rp => rp.PermissionKey)
               .Select(rp => rp.PermissionKey)
               .ToListAsync(cancellationToken);
        }

        public async Task<bool> ReplaceRolePermissionsAsync(
            Guid roleId,
            IReadOnlyCollection<string> permissionKeys,
            CancellationToken cancellationToken)
        {
            List<RolePermission> existing = await _db.RolePermissions
               .Where(rp => rp.RoleId == roleId)
               .ToListAsync(cancellationToken);

            HashSet<string> desired = permissionKeys.Count == 0
                ? new HashSet<string>()
                : permissionKeys.ToHashSet();

            HashSet<string> existingKeys = existing.Count == 0
                ? new HashSet<string>()
                : existing.Select(x => x.PermissionKey)
                   .ToHashSet();

            var toRemove = existing
               .Where(rp => !desired.Contains(rp.PermissionKey))
               .ToList();

            var toAdd = desired
               .Where(key => !existingKeys.Contains(key))
               .Select(key => new RolePermission(
                    roleId: roleId,
                    permissionKey: key))
               .ToList();

            bool changed = toRemove.Count > 0 || toAdd.Count > 0;
            if (!changed)
                return false;

            if (toRemove.Count > 0)
                _db.RolePermissions.RemoveRange(toRemove);

            if (toAdd.Count > 0)
                await _db.RolePermissions.AddRangeAsync(
                    entities: toAdd,
                    cancellationToken: cancellationToken);

            return true;
        }
    }
}
