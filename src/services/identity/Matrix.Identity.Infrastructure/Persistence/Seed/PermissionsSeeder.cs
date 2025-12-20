using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Seed
{
    public sealed class PermissionsSeeder(IdentityDbContext db)
    {
        private readonly IdentityDbContext _db = db;

        public async Task SeedAsync(CancellationToken ct)
        {
            // Load current permissions from DB
            Dictionary<string, Permission> existing = await _db.Permissions
               .ToDictionaryAsync(
                    keySelector: x => x.Key,
                    cancellationToken: ct);

            // Upsert from catalog
            foreach (PermissionDefinition def in PermissionsCatalog.All)
                if (existing.TryGetValue(
                        key: def.Key,
                        value: out Permission? permission))
                {
                    permission.UpdateMetadata(
                        service: def.Service,
                        group: def.Group,
                        description: def.Description);
                    permission.Activate();
                }
                else
                    _db.Permissions.Add(
                        new Permission(
                            key: def.Key,
                            service: def.Service,
                            group: def.Group,
                            description: def.Description));

            // Deprecate permissions missing from catalog (do not delete!)
            var catalogKeys = PermissionsCatalog.All.Select(x => x.Key)
               .ToHashSet(StringComparer.Ordinal);

            foreach (KeyValuePair<string, Permission> kv in existing)
                if (!catalogKeys.Contains(kv.Key))
                    kv.Value.Deprecate();

            await _db.SaveChangesAsync(ct);
        }
    }
}
