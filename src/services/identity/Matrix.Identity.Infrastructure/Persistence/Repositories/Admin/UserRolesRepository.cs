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

        public async Task<IReadOnlyCollection<UserRoleResult>> GetUserRolesAsync(
            Guid userId,
            CancellationToken ct)
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
               .ToListAsync(ct);
        }

        public async Task ReplaceUserRolesAsync(
            Guid userId,
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken ct)
        {
            List<UserRole> existing = await _db.UserRoles
               .Where(ur => ur.UserId == userId)
               .ToListAsync(ct);

            var desired = new HashSet<Guid>(roleIds);

            List<UserRole> toRemove = existing
               .Where(ur => !desired.Contains(ur.RoleId))
               .ToList();

            List<Guid> existingRoleIds = existing
               .Select(ur => ur.RoleId)
               .ToList();

            List<UserRole> toAdd = desired
               .Except(existingRoleIds)
               .Select(roleId => new UserRole(userId, roleId))
               .ToList();

            if (toRemove.Count > 0)
                _db.UserRoles.RemoveRange(toRemove);

            if (toAdd.Count > 0)
                await _db.UserRoles.AddRangeAsync(toAdd, ct);
        }
    }
}
