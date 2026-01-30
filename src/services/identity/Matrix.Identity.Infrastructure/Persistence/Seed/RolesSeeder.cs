using Matrix.Identity.Contracts.Authorization.Permissions;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using PopulationPermissionKeys = Matrix.Population.Contracts.Authorization.Permissions.PermissionKeys;

namespace Matrix.Identity.Infrastructure.Persistence.Seed
{
    public sealed class RolesSeeder(IdentityDbContext db)
    {
        private readonly IdentityDbContext _db = db;

        public async Task SeedSystemRolesAsync(CancellationToken cancellationToken)
        {
            Dictionary<string, Role> rolesByName = await EnsureSystemRolesAsync(cancellationToken);

            List<string> activePermissionKeys = await _db.Permissions
               .Where(p => !p.IsDeprecated)
               .Select(p => p.Key)
               .ToListAsync(cancellationToken);

            var activePermissionKeySet = activePermissionKeys.ToHashSet(StringComparer.Ordinal);

            bool superAdminChanged = await SyncRolePermissionsAsync(
                roleId: rolesByName[SystemRoleNames.SuperAdmin].Id,
                desiredPermissionKeys: activePermissionKeys,
                cancellationToken: cancellationToken);

            bool adminChanged = await SyncRolePermissionsAsync(
                roleId: rolesByName[SystemRoleNames.Admin].Id,
                desiredPermissionKeys: activePermissionKeys,
                cancellationToken: cancellationToken);

            var defaultUserPermissionKeys = GetDefaultUserPermissionKeys()
               .Where(activePermissionKeySet.Contains)
               .ToList();

            bool userChanged = await SyncRolePermissionsAsync(
                roleId: rolesByName[SystemRoleNames.User].Id,
                desiredPermissionKeys: defaultUserPermissionKeys,
                cancellationToken: cancellationToken);

            if (superAdminChanged)
                await BumpPermissionsVersionByRoleAsync(
                    roleId: rolesByName[SystemRoleNames.SuperAdmin].Id,
                    cancellationToken: cancellationToken);

            if (adminChanged)
                await BumpPermissionsVersionByRoleAsync(
                    roleId: rolesByName[SystemRoleNames.Admin].Id,
                    cancellationToken: cancellationToken);

            if (userChanged)
                await BumpPermissionsVersionByRoleAsync(
                    roleId: rolesByName[SystemRoleNames.User].Id,
                    cancellationToken: cancellationToken);
        }

        private async Task<Dictionary<string, Role>> EnsureSystemRolesAsync(CancellationToken cancellationToken)
        {
            var systemRoleNames = new List<string>
            {
                SystemRoleNames.SuperAdmin,
                SystemRoleNames.Admin,
                SystemRoleNames.User
            };

            List<Role> existingRoles = await _db.Roles
               .Where(role => systemRoleNames.Contains(role.Name))
               .ToListAsync(cancellationToken);

            var rolesByName = existingRoles.ToDictionary(
                keySelector: role => role.Name,
                comparer: StringComparer.Ordinal);

            bool rolesChanged = false;

            foreach (string roleName in systemRoleNames)
            {
                if (!rolesByName.TryGetValue(
                        key: roleName,
                        value: out Role? role))
                {
                    role = Role.Create(
                        name: roleName,
                        isSystem: true);

                    _db.Roles.Add(role);
                    rolesByName.Add(
                        key: roleName,
                        value: role);
                    rolesChanged = true;
                    continue;
                }

                if (!role.IsSystem)
                {
                    role.MarkAsSystem();
                    rolesChanged = true;
                }
            }

            if (rolesChanged)
                await _db.SaveChangesAsync(cancellationToken);

            return rolesByName;
        }

        private async Task<bool> SyncRolePermissionsAsync(
            Guid roleId,
            IReadOnlyCollection<string> desiredPermissionKeys,
            CancellationToken cancellationToken)
        {
            List<string> existingKeys = await _db.RolePermissions
               .Where(rp => rp.RoleId == roleId)
               .Select(rp => rp.PermissionKey)
               .ToListAsync(cancellationToken);

            var toAdd = desiredPermissionKeys
               .Except(
                    second: existingKeys,
                    comparer: StringComparer.Ordinal)
               .Select(permissionKey => new RolePermission(
                    roleId: roleId,
                    permissionKey: permissionKey))
               .ToList();

            var toRemoveKeys = existingKeys
               .Except(
                    second: desiredPermissionKeys,
                    comparer: StringComparer.Ordinal)
               .ToList();

            List<RolePermission> toRemove = toRemoveKeys.Count == 0
                ? new List<RolePermission>()
                : await _db.RolePermissions
                   .Where(rp => rp.RoleId == roleId && toRemoveKeys.Contains(rp.PermissionKey))
                   .ToListAsync(cancellationToken);

            bool changed = toAdd.Count > 0 || toRemove.Count > 0;

            if (!changed)
                return false;

            if (toAdd.Count > 0)
                _db.RolePermissions.AddRange(toAdd);

            if (toRemove.Count > 0)
                _db.RolePermissions.RemoveRange(toRemove);

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task BumpPermissionsVersionByRoleAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            await _db.Users
               .Where(user => _db.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RoleId == roleId))
               .ExecuteUpdateAsync(
                    setPropertyCalls: setters => setters.SetProperty(
                        user => user.PermissionsVersion,
                        user => user.PermissionsVersion + 1),
                    cancellationToken: cancellationToken);
        }

        private static List<string> GetDefaultUserPermissionKeys()
        {
            return new List<string>
            {
                PermissionKeys.IdentityMeProfileRead,
                PermissionKeys.IdentityMePasswordChange,
                PermissionKeys.IdentityMeAvatarChange,
                PermissionKeys.IdentityMeSessionsRead,
                PermissionKeys.IdentityMeSessionsRevoke,
                PermissionKeys.IdentityMeSessionsRevokeAll,
                PopulationPermissionKeys.PopulationPeopleRead
            };
        }
    }
}
