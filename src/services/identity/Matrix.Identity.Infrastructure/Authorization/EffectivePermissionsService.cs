using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Authorization
{
    public sealed class EffectivePermissionsService(IdentityDbContext db) : IEffectivePermissionsService
    {
        private readonly IdentityDbContext _db = db;

        public async Task<AuthorizationContext> GetAuthContextAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            List<Guid> roleIds = await _db.UserRoles
               .AsNoTracking()
               .Where(userRole => userRole.UserId == userId)
               .Select(userRole => userRole.RoleId)
               .Distinct()
               .ToListAsync(cancellationToken);

            List<string> roles = roleIds.Count == 0
                ? new List<string>()
                : await _db.Roles
                   .AsNoTracking()
                   .Where(role => roleIds.Contains(role.Id))
                   .Select(role => role.Name)
                   .Distinct()
                   .ToListAsync(cancellationToken);

            bool isSuperAdmin = roles.Any(role => string.Equals(
                a: role,
                b: SystemRoleNames.SuperAdmin,
                comparisonType: StringComparison.Ordinal));

            List<string> rolePermissionKeys;
            if (isSuperAdmin)
                rolePermissionKeys = await _db.Permissions
                   .AsNoTracking()
                   .Where(permission => !permission.IsDeprecated)
                   .Select(permission => permission.Key)
                   .Distinct()
                   .ToListAsync(cancellationToken);
            else
                if (roleIds.Count == 0)
                    rolePermissionKeys = new List<string>();
                else
                    rolePermissionKeys = await (from rolePermission in _db.RolePermissions.AsNoTracking()
                                                join permission in _db.Permissions.AsNoTracking()
                                                    on rolePermission.PermissionKey equals permission.Key
                                                where roleIds.Contains(rolePermission.RoleId) &&
                                                      !permission.IsDeprecated
                                                select rolePermission.PermissionKey)
                       .Distinct()
                       .ToListAsync(cancellationToken);

            var effective = new HashSet<string>(
                collection: rolePermissionKeys,
                comparer: StringComparer.Ordinal);

            if (!isSuperAdmin)
            {
                var overrides = await (from userOverride in _db.UserPermissionOverrides.AsNoTracking()
                                       join permission in _db.Permissions.AsNoTracking()
                                           on userOverride.PermissionKey equals permission.Key
                                       where userOverride.UserId == userId && !permission.IsDeprecated
                                       select new
                                       {
                                           userOverride.PermissionKey,
                                           userOverride.Effect
                                       })
                   .ToListAsync(cancellationToken);

                foreach (var userOverride in overrides)
                    if (userOverride.Effect == PermissionEffect.Deny)
                        effective.Remove(userOverride.PermissionKey);

                foreach (var userOverride in overrides)
                    if (userOverride.Effect == PermissionEffect.Allow)
                        effective.Add(userOverride.PermissionKey);
            }

            int permissionsVersion = await _db.Users
               .AsNoTracking()
               .Where(user => user.Id == userId)
               .Select(user => user.PermissionsVersion)
               .SingleAsync(cancellationToken);

            return new AuthorizationContext(
                Roles: roles,
                Permissions: effective.ToList(),
                PermissionsVersion: permissionsVersion);
        }
    }
}
