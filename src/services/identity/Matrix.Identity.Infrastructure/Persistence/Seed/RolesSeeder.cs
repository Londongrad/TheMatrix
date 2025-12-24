using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Seed
{
    public sealed class RolesSeeder(IdentityDbContext db)
    {
        private readonly IdentityDbContext _db = db;

        public async Task SeedAdminRoleWithAllPermissionsAsync(CancellationToken cancellationToken)
        {
            // 1) Ensure Admin role exists
            Role? adminRole = await _db.Roles.FirstOrDefaultAsync(
                predicate: r => r.Name == SystemRoleNames.Admin,
                cancellationToken: cancellationToken);
            if (adminRole is null)
            {
                adminRole = Role.Create(
                    name: SystemRoleNames.Admin,
                    isSystem: true);
                _db.Roles.Add(adminRole);
                await _db.SaveChangesAsync(cancellationToken);
            }

            // 2) Sync Admin role permissions = all active permissions
            List<string> activePermissionKeys = await _db.Permissions
               .Where(p => !p.IsDeprecated)
               .Select(p => p.Key)
               .ToListAsync(cancellationToken);

            List<string> existingKeys = await _db.RolePermissions
               .Where(rp => rp.RoleId == adminRole.Id)
               .Select(rp => rp.PermissionKey)
               .ToListAsync(cancellationToken);

            var toAdd = activePermissionKeys
               .Except(
                    second: existingKeys,
                    comparer: StringComparer.Ordinal)
               .Select(k => new RolePermission(
                    roleId: adminRole!.Id,
                    permissionKey: k))
               .ToList();

            if (toAdd.Count > 0)
                _db.RolePermissions.AddRange(toAdd);

            var toRemoveKeys = existingKeys
               .Except(
                    second: activePermissionKeys,
                    comparer: StringComparer.Ordinal)
               .ToList();

            if (toRemoveKeys.Count > 0)
            {
                List<RolePermission> toRemove = await _db.RolePermissions
                   .Where(rp => rp.RoleId == adminRole.Id && toRemoveKeys.Contains(rp.PermissionKey))
                   .ToListAsync(cancellationToken);

                _db.RolePermissions.RemoveRange(toRemove);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
