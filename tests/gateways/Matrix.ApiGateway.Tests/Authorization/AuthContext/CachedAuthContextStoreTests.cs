using System.Text.Json;
using Matrix.ApiGateway.Authorization.AuthContext;
using Matrix.ApiGateway.Authorization.Caching;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Authorization.AuthContext;

public sealed class CachedAuthContextStoreTests
{
    [Fact]
    public async Task GetAsync_WhenValidCacheEntryExists_ReturnsCachedContextWithoutIdentityCall()
    {
        Guid userId = Guid.Parse("5b4499d1-5946-4932-9db4-c1ac72f79dbf");
        const int permissionsVersion = 9;
        var cache = new RecordingDistributedCache();
        var client = new FakeIdentityInternalUsersClient();
        UserAuthContextResponse expected = new(
            PermissionsVersion: permissionsVersion,
            EffectivePermissions: ["city.read", "city.write"]);
        cache.SeedString(
            key: AuthorizationCacheKeys.AuthContext(userId, permissionsVersion),
            value: JsonSerializer.Serialize(expected));
        var store = new CachedAuthContextStore(
            distributedCache: cache,
            client: client,
            options: CreateAuthContextOptions(),
            logger: NullLogger<CachedAuthContextStore>.Instance);

        UserAuthContextResponse actual = await store.GetAsync(
            userId: userId,
            permissionsVersion: permissionsVersion,
            cancellationToken: CancellationToken.None);

        Assert.Equal(expected.PermissionsVersion, actual.PermissionsVersion);
        Assert.Equal(expected.EffectivePermissions, actual.EffectivePermissions);
        Assert.Equal(0, client.GetAuthContextCallCount);
        Assert.Empty(cache.WrittenOptions);
    }

    [Fact]
    public async Task GetAsync_WhenCacheMissOccurs_LoadsFromIdentityAndWritesCache()
    {
        Guid userId = Guid.Parse("0c2e7fcc-8f5a-4daa-ae34-cd6f18357b44");
        const int permissionsVersion = 4;
        var cache = new RecordingDistributedCache();
        var client = new FakeIdentityInternalUsersClient();
        UserAuthContextResponse expected = new(
            PermissionsVersion: permissionsVersion,
            EffectivePermissions: ["admin.manage", "city.read"]);
        client.UserAuthContexts[userId] = expected;
        var store = new CachedAuthContextStore(
            distributedCache: cache,
            client: client,
            options: CreateAuthContextOptions(cacheTtlSeconds: 321),
            logger: NullLogger<CachedAuthContextStore>.Instance);

        UserAuthContextResponse actual = await store.GetAsync(
            userId: userId,
            permissionsVersion: permissionsVersion,
            cancellationToken: CancellationToken.None);

        string cacheKey = AuthorizationCacheKeys.AuthContext(userId, permissionsVersion);

        Assert.Equal(expected, actual);
        Assert.Equal(1, client.GetAuthContextCallCount);
        Assert.Equal(JsonSerializer.Serialize(expected), cache.ReadString(cacheKey));
        Assert.Equal(TimeSpan.FromSeconds(321), cache.WrittenOptions[cacheKey].AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task GetAsync_WhenCachedEntryIsInvalid_FallsBackToIdentity()
    {
        Guid userId = Guid.Parse("cf47a4e9-b994-4f08-8fb0-5c94479a0a9a");
        const int permissionsVersion = 12;
        var cache = new RecordingDistributedCache();
        var client = new FakeIdentityInternalUsersClient();
        cache.SeedString(
            key: AuthorizationCacheKeys.AuthContext(userId, permissionsVersion),
            value: JsonSerializer.Serialize(new UserAuthContextResponse(
                PermissionsVersion: permissionsVersion,
                EffectivePermissions: [])));
        UserAuthContextResponse expected = new(
            PermissionsVersion: permissionsVersion,
            EffectivePermissions: ["population.read"]);
        client.UserAuthContexts[userId] = expected;
        var store = new CachedAuthContextStore(
            distributedCache: cache,
            client: client,
            options: CreateAuthContextOptions(),
            logger: NullLogger<CachedAuthContextStore>.Instance);

        UserAuthContextResponse actual = await store.GetAsync(
            userId: userId,
            permissionsVersion: permissionsVersion,
            cancellationToken: CancellationToken.None);

        Assert.Equal(expected, actual);
        Assert.Equal(1, client.GetAuthContextCallCount);
    }
}
