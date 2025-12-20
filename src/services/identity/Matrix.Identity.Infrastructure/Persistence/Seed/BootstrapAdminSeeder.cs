using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Seed
{
    public sealed class BootstrapAdminSeeder(IdentityDbContext db)
    {
        private readonly IdentityDbContext _db = db;

        public async Task EnsureAtLeastOneAdminAsync(CancellationToken ct)
        {
            Role? adminRole = await _db.Roles.FirstOrDefaultAsync(
                predicate: r => r.Name == SystemRoleNames.Admin,
                cancellationToken: ct);

            if (adminRole is null)
                return; // role seeder should run first

            bool anyAdmin = await _db.UserRoles.AnyAsync(
                predicate: ur => ur.RoleId == adminRole.Id,
                cancellationToken: ct);

            if (anyAdmin)
                return;

            Guid firstUserId = await _db.Users
               .OrderBy(u => u.CreatedAtUtc)
               .Select(u => u.Id)
               .FirstOrDefaultAsync(ct);

            if (firstUserId == Guid.Empty)
                return; // no users yet

            _db.UserRoles.Add(
                new UserRole(
                    userId: firstUserId,
                    roleId: adminRole.Id));

            await _db.SaveChangesAsync(ct);
        }
    }
}
