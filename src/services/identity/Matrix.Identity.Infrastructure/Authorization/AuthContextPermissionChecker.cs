using System.Security.Claims;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Microsoft.AspNetCore.Http;

namespace Matrix.Identity.Infrastructure.Authorization
{
    public sealed class AuthContextPermissionChecker(
        IEffectivePermissionsService authContext,
        IHttpContextAccessor httpContextAccessor) : IPermissionChecker
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

            // 1) Fast-path: permissions already came via Internal JWT as claims "perm"
            ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;
            if (principal is not null)
            {
                string[] fromClaims = principal
                   .FindAll(JwtClaimNames.Permission)
                   .Select(x => x.Value)
                   .Where(x => !string.IsNullOrWhiteSpace(x))
                   .Distinct(StringComparer.Ordinal)
                   .ToArray();

                if (fromClaims.Length > 0)
                {
                    _cachedPermissions = new HashSet<string>(
                        collection: fromClaims,
                        comparer: StringComparer.Ordinal);
                    _cachedUserId = userId;
                    return _cachedPermissions;
                }
            }

            // 2) Fallback: compute from DB through existing service
            AuthorizationContext ctx = await authContext.GetAuthContextAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            _cachedPermissions = new HashSet<string>(
                collection: ctx.Permissions,
                comparer: StringComparer.Ordinal);
            _cachedUserId = userId;

            return _cachedPermissions;
        }
    }
}
