using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Microsoft.AspNetCore.Http;

namespace Matrix.BuildingBlocks.Infrastructure.Authorization.Claims
{
    public sealed class ClaimsPermissionChecker(IHttpContextAccessor httpContextAccessor) : IPermissionChecker
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        private HashSet<string>? _cachedPermissions;
        private Guid? _cachedUserId;

        public Task<bool> HasAsync(
            Guid userId,
            string permissionKey,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
                return Task.FromResult(false);

            HashSet<string> permissions = GetPermissions(userId);
            return Task.FromResult(permissions.Contains(permissionKey));
        }

        private HashSet<string> GetPermissions(Guid userId)
        {
            if (_cachedPermissions is not null && _cachedUserId == userId)
                return _cachedPermissions;

            string[] fromClaims = _httpContextAccessor.HttpContext?.User
               .FindAll(JwtClaimNames.Permission)
               .Select(x => x.Value)
               .Where(x => !string.IsNullOrWhiteSpace(x))
               .Distinct(StringComparer.Ordinal)
               .ToArray() ?? [];

            _cachedPermissions = new HashSet<string>(
                collection: fromClaims,
                comparer: StringComparer.Ordinal);

            _cachedUserId = userId;

            return _cachedPermissions;
        }
    }
}
