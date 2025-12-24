using Matrix.Identity.Application.Abstractions.Services.Authorization;
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
            // 1) RoleIds пользователя
            List<Guid> roleIds = await _db.UserRoles
               .AsNoTracking()
               .Where(ur => ur.UserId == userId)
               .Select(ur => ur.RoleId)
               .Distinct()
               .ToListAsync(cancellationToken);

            // 2) Имена ролей (для claims)
            List<string> roles = roleIds.Count == 0
                ? []
                : await _db.Roles
                   .AsNoTracking()
                   .Where(r => roleIds.Contains(r.Id))
                   .Select(r => r.Name)
                   .Distinct()
                   .ToListAsync(cancellationToken);

            // 3) Permissions из ролей (только активные permissions)
            List<string> rolePermissionKeys = roleIds.Count == 0
                ? []
                : await (from rp in _db.RolePermissions.AsNoTracking()
                         join p in _db.Permissions.AsNoTracking() on rp.PermissionKey equals p.Key
                         where roleIds.Contains(rp.RoleId) && !p.IsDeprecated
                         select rp.PermissionKey)
                   .Distinct()
                   .ToListAsync(cancellationToken);

            var effective = new HashSet<string>(
                collection: rolePermissionKeys,
                comparer: StringComparer.Ordinal);

            // 4) User overrides (Allow/Deny) тоже только по активным permissions
            var overrides = await (from o in _db.UserPermissionOverrides.AsNoTracking()
                                   join p in _db.Permissions.AsNoTracking() on o.PermissionKey equals p.Key
                                   where o.UserId == userId && !p.IsDeprecated
                                   select new
                                   {
                                       o.PermissionKey,
                                       o.Effect
                                   })
               .ToListAsync(cancellationToken);

            foreach (var o in overrides)
                if (o.Effect == PermissionEffect.Deny)
                    effective.Remove(o.PermissionKey);

            foreach (var o in overrides)
                if (o.Effect == PermissionEffect.Allow)
                    effective.Add(o.PermissionKey);

            // 5) PermissionsVersion
            int pv = await _db.Users
               .AsNoTracking()
               .Where(u => u.Id == userId)
               .Select(u => u.PermissionsVersion)
               .SingleAsync(cancellationToken);

            return new AuthorizationContext(
                Roles: roles,
                Permissions: effective.ToList(),
                PermissionsVersion: pv);
        }
    }
}
