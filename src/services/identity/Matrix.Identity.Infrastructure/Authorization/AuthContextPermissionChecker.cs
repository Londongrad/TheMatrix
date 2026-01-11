using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Abstractions.Services.Authorization;

namespace Matrix.Identity.Infrastructure.Authorization
{
    public sealed class AuthContextPermissionChecker(IEffectivePermissionsService authContext) : IPermissionChecker
    {
        private HashSet<string>? _cachedPermissions;
        private Guid? _cachedUserId;

        public async Task<bool> HasAsync(
            Guid userId,
            string permissionKey,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
                return false;

            HashSet<string> permissions = await GetPermissionsAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            return permissions.Contains(permissionKey);
        }

        private async Task<HashSet<string>> GetPermissionsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            if (_cachedPermissions is not null && _cachedUserId == userId)
                return _cachedPermissions;

            AuthorizationContext ctx = await authContext.GetAuthContextAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            _cachedPermissions = ctx.Permissions is HashSet<string> hs
                ? new HashSet<string>(
                    collection: hs,
                    comparer: StringComparer.Ordinal)
                : new HashSet<string>(
                    collection: ctx.Permissions,
                    comparer: StringComparer.Ordinal);

            _cachedUserId = userId;

            return _cachedPermissions;
        }
    }
}
