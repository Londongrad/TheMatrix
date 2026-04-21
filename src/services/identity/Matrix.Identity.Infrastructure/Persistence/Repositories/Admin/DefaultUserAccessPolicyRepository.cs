using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class DefaultUserAccessPolicyRepository(
        IdentityDbContext db,
        IClock clock)
        : IDefaultUserAccessPolicyRepository
    {
        private readonly IdentityDbContext _db = db;
        private readonly IClock _clock = clock;

        public async Task<DefaultUserAccessPolicy> GetForUpdateAsync(CancellationToken cancellationToken)
        {
            DefaultUserAccessPolicy? policy = await _db.DefaultUserAccessPolicies
               .FirstOrDefaultAsync(
                    predicate: x => x.Id == DefaultUserAccessPolicy.SingletonId,
                    cancellationToken: cancellationToken);

            if (policy is not null)
                return policy;

            policy = DefaultUserAccessPolicy.CreateDefault(_clock.UtcNow);
            await _db.DefaultUserAccessPolicies.AddAsync(
                entity: policy,
                cancellationToken: cancellationToken);

            return policy;
        }

        public async Task<int> GetVersionAsync(CancellationToken cancellationToken)
        {
            int? version = await _db.DefaultUserAccessPolicies
               .AsNoTracking()
               .Where(x => x.Id == DefaultUserAccessPolicy.SingletonId)
               .Select(x => (int?)x.Version)
               .FirstOrDefaultAsync(cancellationToken);

            return version ?? 1;
        }

        public async Task<IReadOnlyDictionary<string, PermissionEffect>> GetOverridesAsync(CancellationToken cancellationToken)
        {
            List<DefaultUserAccessOverride> overrides = await _db.DefaultUserAccessOverrides
               .AsNoTracking()
               .Where(x => x.PolicyId == DefaultUserAccessPolicy.SingletonId)
               .OrderBy(x => x.PermissionKey)
               .ToListAsync(cancellationToken);

            return overrides.ToDictionary(
                keySelector: x => x.PermissionKey,
                elementSelector: x => x.Effect,
                comparer: StringComparer.Ordinal);
        }

        public async Task<bool> ReplaceOverridesAsync(
            IReadOnlyDictionary<string, PermissionEffect> overrides,
            CancellationToken cancellationToken)
        {
            _ = await GetForUpdateAsync(cancellationToken);

            List<DefaultUserAccessOverride> existing = await _db.DefaultUserAccessOverrides
               .Where(x => x.PolicyId == DefaultUserAccessPolicy.SingletonId)
               .ToListAsync(cancellationToken);

            Dictionary<string, PermissionEffect> desired = overrides.Count == 0
                ? new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
                : overrides.ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.Value,
                    comparer: StringComparer.Ordinal);

            var existingByKey = existing.ToDictionary(
                keySelector: x => x.PermissionKey,
                comparer: StringComparer.Ordinal);

            var toRemove = existing
               .Where(entry => !desired.ContainsKey(entry.PermissionKey))
               .ToList();

            var toAdd = desired
               .Where(entry => !existingByKey.ContainsKey(entry.Key))
               .Select(entry => new DefaultUserAccessOverride(
                    policyId: DefaultUserAccessPolicy.SingletonId,
                    permissionKey: entry.Key,
                    effect: entry.Value))
               .ToList();

            var toUpdate = existing
               .Where(entry =>
                    desired.TryGetValue(
                        key: entry.PermissionKey,
                        value: out PermissionEffect desiredEffect) &&
                    entry.Effect != desiredEffect)
               .ToList();

            bool changed = toRemove.Count > 0 || toAdd.Count > 0 || toUpdate.Count > 0;
            if (!changed)
                return false;

            foreach (DefaultUserAccessOverride entry in toUpdate)
                entry.SetEffect(desired[entry.PermissionKey]);

            if (toRemove.Count > 0)
                _db.DefaultUserAccessOverrides.RemoveRange(toRemove);

            if (toAdd.Count > 0)
                await _db.DefaultUserAccessOverrides.AddRangeAsync(
                    entities: toAdd,
                    cancellationToken: cancellationToken);

            return true;
        }
    }
}
