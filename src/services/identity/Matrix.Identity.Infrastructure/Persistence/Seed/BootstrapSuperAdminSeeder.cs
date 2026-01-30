using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Seed
{
    public sealed class BootstrapSuperAdminSeeder(IdentityDbContext db)
    {
        private readonly IdentityDbContext _db = db;

        public async Task EnsureAtLeastOneSuperAdminAsync(CancellationToken cancellationToken)
        {
            Role? superAdminRole = await _db.Roles.FirstOrDefaultAsync(
                predicate: role => role.Name == SystemRoleNames.SuperAdmin,
                cancellationToken: cancellationToken);

            if (superAdminRole is null)
                return;

            bool anySuperAdmin = await _db.UserRoles.AnyAsync(
                predicate: userRole => userRole.RoleId == superAdminRole.Id,
                cancellationToken: cancellationToken);

            if (anySuperAdmin)
                return;

            Guid firstUserId = await _db.Users
               .OrderBy(user => user.CreatedAtUtc)
               .Select(user => user.Id)
               .FirstOrDefaultAsync(cancellationToken);

            if (firstUserId == Guid.Empty)
                return;

            _db.UserRoles.Add(
                new UserRole(
                    userId: firstUserId,
                    roleId: superAdminRole.Id));

            await _db.SaveChangesAsync(cancellationToken);

            await _db.Users
               .Where(user => user.Id == firstUserId)
               .ExecuteUpdateAsync(
                    setPropertyCalls: setters => setters.SetProperty(
                        user => user.PermissionsVersion,
                        user => user.PermissionsVersion + 1),
                    cancellationToken: cancellationToken);
        }
    }
}
