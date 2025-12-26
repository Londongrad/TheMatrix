using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories.Admin
{
    public sealed class UserRolesRepository(IdentityDbContext db)
        : IUserRolesRepository
    {
        private readonly IdentityDbContext _db = db;

        public async Task<bool> ReplaceUserRolesAsync(
            Guid userId,
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken)
        {
            List<UserRole> existing = await _db.UserRoles
               .Where(ur => ur.UserId == userId)
               .ToListAsync(cancellationToken);

            // desired set (уникальные роли)
            HashSet<Guid> desired = roleIds.Count == 0
                ? new HashSet<Guid>()
                : roleIds.ToHashSet();

            // existing role ids set
            HashSet<Guid> existingRoleIds = existing.Count == 0
                ? new HashSet<Guid>()
                : existing.Select(x => x.RoleId)
                   .ToHashSet();

            // what to remove (entities)
            var toRemove = existing
               .Where(ur => !desired.Contains(ur.RoleId))
               .ToList();

            // what to add (entities)
            var toAdd = desired
               .Where(roleId => !existingRoleIds.Contains(roleId))
               .Select(roleId => new UserRole(
                    userId: userId,
                    roleId: roleId))
               .ToList();

            bool changed = toRemove.Count > 0 || toAdd.Count > 0;
            if (!changed)
                return false;

            if (toRemove.Count > 0)
                _db.UserRoles.RemoveRange(toRemove);

            if (toAdd.Count > 0)
                await _db.UserRoles.AddRangeAsync(
                    entities: toAdd,
                    cancellationToken: cancellationToken);

            return true;
        }

        public async Task<IReadOnlyCollection<UserRoleResult>> GetUserRolesAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return await (from ur in _db.UserRoles.AsNoTracking()
                          join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                          where ur.UserId == userId
                          orderby r.Name
                          select new UserRoleResult
                          {
                              Id = r.Id,
                              Name = r.Name,
                              IsSystem = r.IsSystem,
                              CreatedAtUtc = r.CreatedAtUtc
                          })
               .ToListAsync(cancellationToken);
        }
    }
}
