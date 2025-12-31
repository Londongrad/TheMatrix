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
            CancellationToken ct)
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
               .ToListAsync(ct);
        }

        public async Task UpsertUserPermissionAsync(
            Guid userId,
            string permissionKey,
            PermissionEffect effect,
            CancellationToken ct)
        {
            UserPermissionOverride? existing = await _db.UserPermissionOverrides
               .FirstOrDefaultAsync(
                    predicate: o => o.UserId == userId && o.PermissionKey == permissionKey,
                    cancellationToken: ct);

            if (existing is null)
            {
                var overrideEntry = new UserPermissionOverride(
                    userId: userId,
                    permissionKey: permissionKey,
                    effect: effect);

                await _db.UserPermissionOverrides.AddAsync(overrideEntry, ct);
                return;
            }

            existing.SetEffect(effect);
        }
    }
}
