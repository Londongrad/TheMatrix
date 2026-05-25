using System.Security.Claims;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Infrastructure.Authorization;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Matrix.Identity.Infrastructure.Tests.Authorization
{
    public sealed class DbFallbackPermissionCheckerTests
    {
        [Fact]
        public async Task HasAsync_WhenClaimExists_UsesFastPathWithoutFallback()
        {
            var userId = Guid.NewGuid();
            var fallback = new StubEffectivePermissionsService(
                new AuthorizationContext(
                    Roles: [],
                    Permissions: ["identity.users.read"],
                    PermissionsVersion: 1));
            DbFallbackPermissionChecker checker = CreateChecker(
                userId: userId,
                permissionClaims: ["identity.users.read"],
                fallback: fallback);

            bool allowed = await checker.HasAsync(
                userId: userId,
                permissionKey: "identity.users.read",
                ct: CancellationToken.None);

            Assert.True(allowed);
            Assert.Equal(
                expected: 0,
                actual: fallback.CallCount);
        }

        [Fact]
        public async Task HasAnyAndHasAllAsync_WhenClaimsMiss_UseFallbackPermissions()
        {
            var userId = Guid.NewGuid();
            var fallback = new StubEffectivePermissionsService(
                new AuthorizationContext(
                    Roles: ["User"],
                    Permissions:
                    [
                        "identity.users.read",
                        "identity.users.write"
                    ],
                    PermissionsVersion: 7));
            DbFallbackPermissionChecker checker = CreateChecker(
                userId: userId,
                permissionClaims: [],
                fallback: fallback);

            bool hasAny = await checker.HasAnyAsync(
                userId: userId,
                permissionKeys:
                [
                    "identity.users.delete",
                    "identity.users.read"
                ],
                ct: CancellationToken.None);
            bool hasAll = await checker.HasAllAsync(
                userId: userId,
                permissionKeys:
                [
                    "identity.users.read",
                    "identity.users.write"
                ],
                ct: CancellationToken.None);

            Assert.True(hasAny);
            Assert.True(hasAll);
            Assert.Equal(
                expected: 2,
                actual: fallback.CallCount);
        }

        private static DbFallbackPermissionChecker CreateChecker(
            Guid userId,
            IReadOnlyCollection<string> permissionClaims,
            StubEffectivePermissionsService fallback)
        {
            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            claims: permissionClaims.Select(permission => new Claim(
                                type: JwtClaimNames.Permission,
                                value: permission)),
                            authenticationType: "test"))
                }
            };

            return new DbFallbackPermissionChecker(
                claimsChecker: new ClaimsPermissionChecker(httpContextAccessor),
                effectivePermissions: fallback);
        }

        private sealed class StubEffectivePermissionsService(AuthorizationContext context)
            : IEffectivePermissionsService
        {
            public int CallCount { get; private set; }

            public Task<AuthorizationContext> GetAuthContextAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(context);
            }
        }
    }
}
