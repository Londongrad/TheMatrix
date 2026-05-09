using System.Security.Claims;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Infrastructure.Authorization;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Matrix.Identity.Infrastructure.Tests.Authorization;

public sealed class DbFallbackPermissionCheckerTests
{
    [Fact]
    public async Task HasAsync_WhenClaimExists_UsesFastPathWithoutFallback()
    {
        Guid userId = Guid.NewGuid();
        var fallback = new StubEffectivePermissionsService(
            new AuthorizationContext(
                Roles: [],
                Permissions: ["identity.users.read"],
                PermissionsVersion: 1));
        var checker = CreateChecker(
            userId,
            permissionClaims: ["identity.users.read"],
            fallback);

        bool allowed = await checker.HasAsync(userId, "identity.users.read", CancellationToken.None);

        Assert.True(allowed);
        Assert.Equal(0, fallback.CallCount);
    }

    [Fact]
    public async Task HasAnyAndHasAllAsync_WhenClaimsMiss_UseFallbackPermissions()
    {
        Guid userId = Guid.NewGuid();
        var fallback = new StubEffectivePermissionsService(
            new AuthorizationContext(
                Roles: [ "User" ],
                Permissions: ["identity.users.read", "identity.users.write"],
                PermissionsVersion: 7));
        var checker = CreateChecker(userId, permissionClaims: [], fallback);

        bool hasAny = await checker.HasAnyAsync(
            userId,
            ["identity.users.delete", "identity.users.read"],
            CancellationToken.None);
        bool hasAll = await checker.HasAllAsync(
            userId,
            ["identity.users.read", "identity.users.write"],
            CancellationToken.None);

        Assert.True(hasAny);
        Assert.True(hasAll);
        Assert.Equal(2, fallback.CallCount);
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
                        permissionClaims.Select(permission => new Claim(JwtClaimNames.Permission, permission)),
                        authenticationType: "test"))
            }
        };

        return new DbFallbackPermissionChecker(
            new ClaimsPermissionChecker(httpContextAccessor),
            fallback);
    }

    private sealed class StubEffectivePermissionsService(AuthorizationContext context) : IEffectivePermissionsService
    {
        public int CallCount { get; private set; }

        public Task<AuthorizationContext> GetAuthContextAsync(Guid userId, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(context);
        }
    }
}
