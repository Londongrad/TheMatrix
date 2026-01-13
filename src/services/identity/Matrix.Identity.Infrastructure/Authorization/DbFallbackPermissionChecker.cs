using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.Identity.Application.Abstractions.Services.Authorization;

namespace Matrix.Identity.Infrastructure.Authorization
{
    public sealed class DbFallbackPermissionChecker(
        ClaimsPermissionChecker claimsChecker,
        IEffectivePermissionsService effectivePermissions)
        : IPermissionChecker
    {
        public async Task<bool> HasAsync(
            Guid userId,
            string permissionKey,
            CancellationToken ct)
        {
            // 1) Fast-path
            if (await claimsChecker.HasAsync(
                    userId: userId,
                    permissionKey: permissionKey,
                    cancellationToken: ct))
                return true;

            // 2) Fallback (Identity only)
            AuthorizationContext ctx = await effectivePermissions.GetAuthContextAsync(
                userId: userId,
                cancellationToken: ct);

            return ctx.Permissions.Contains(
                value: permissionKey,
                comparer: StringComparer.Ordinal);
        }
    }
}
