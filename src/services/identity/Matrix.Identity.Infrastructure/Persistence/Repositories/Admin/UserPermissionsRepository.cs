using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class UserPermissionsRepository(IdentityDbContext db)
        : IUserPermissionsRepository
    {
        private readonly IdentityDbContext _db = db;

        public async Task<IReadOnlyCollection<UserPermissionOverrideResult>> GetUserPermissionsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return await _db.UserPermissionOverrides
               .AsNoTracking()
               .Where(o => o.UserId == userId)
               .OrderBy(o => o.PermissionKey)
               .Select(o => new UserPermissionOverrideResult
                {
                    PermissionKey = o.PermissionKey,
                    Effect = o.Effect
                })
               .ToListAsync(cancellationToken);
        }

        public async Task<bool> UpsertUserPermissionAsync(
            Guid userId,
            string permissionKey,
            PermissionEffect effect,
            CancellationToken cancellationToken)
        {
            UserPermissionOverride? existing = await _db.UserPermissionOverrides
               .FirstOrDefaultAsync(
                    predicate: o => o.UserId == userId && o.PermissionKey == permissionKey,
                    cancellationToken: cancellationToken);

            if (existing is null)
            {
                var overrideEntry = new UserPermissionOverride(
                    userId: userId,
                    permissionKey: permissionKey,
                    effect: effect);

                await _db.UserPermissionOverrides.AddAsync(
                    entity: overrideEntry,
                    cancellationToken: cancellationToken);
                return true;
            }

            if (existing.Effect == effect)
                return false;

            existing.SetEffect(effect);
            return true;
        }

        public async Task<bool> ReplaceUserPermissionsAsync(
            Guid userId,
            IReadOnlyDictionary<string, PermissionEffect> permissionEffects,
            CancellationToken cancellationToken)
        {
            List<UserPermissionOverride> existing = await _db.UserPermissionOverrides
               .Where(o => o.UserId == userId)
               .ToListAsync(cancellationToken);

            Dictionary<string, PermissionEffect> desired = permissionEffects.Count == 0
                ? new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
                : new Dictionary<string, PermissionEffect>(permissionEffects, StringComparer.Ordinal);

            var existingByKey = existing.ToDictionary(x => x.PermissionKey, StringComparer.Ordinal);

            var toRemove = existing
               .Where(entry => !desired.ContainsKey(entry.PermissionKey))
               .ToList();

            var toAdd = desired
               .Where(entry => !existingByKey.ContainsKey(entry.Key))
               .Select(entry => new UserPermissionOverride(
                    userId: userId,
                    permissionKey: entry.Key,
                    effect: entry.Value))
               .ToList();

            var toUpdate = existing
               .Where(entry =>
                    desired.TryGetValue(entry.PermissionKey, out PermissionEffect desiredEffect) &&
                    entry.Effect != desiredEffect)
               .ToList();

            bool changed = toRemove.Count > 0 || toAdd.Count > 0 || toUpdate.Count > 0;
            if (!changed)
                return false;

            foreach (UserPermissionOverride entry in toUpdate)
                entry.SetEffect(desired[entry.PermissionKey]);

            if (toRemove.Count > 0)
                _db.UserPermissionOverrides.RemoveRange(toRemove);

            if (toAdd.Count > 0)
                await _db.UserPermissionOverrides.AddRangeAsync(
                    entities: toAdd,
                    cancellationToken: cancellationToken);

            return true;
        }
    }
}
