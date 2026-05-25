using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.Authorization.PermissionsVersion;
using Matrix.Identity.Contracts.Internal.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Authorization.PermissionsVersion
{
    public sealed class CachedPermissionsVersionStoreTests
    {
        [Fact]
        public async Task GetCurrentAsync_WhenFreshCacheEntriesExist_ComposesWithoutIdentityCalls()
        {
            var userId = Guid.Parse("349c1f4f-a8d0-4d86-af7b-9e5922cf679b");
            var cache = new RecordingDistributedCache();
            var client = new FakeIdentityInternalUsersClient();
            cache.SeedString(
                key: AuthorizationCacheKeys.PermissionsVersion(userId),
                value: "7");
            cache.SeedString(
                key: AuthorizationCacheKeys.DefaultUserAccessVersion(),
                value: "3");
            var store = new CachedPermissionsVersionStore(
                distributedCache: cache,
                client: client,
                options: CreatePermissionsVersionOptions(),
                logger: NullLogger<CachedPermissionsVersionStore>.Instance);

            int actual = await store.GetCurrentAsync(
                userId: userId,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: PermissionsVersionComposer.Compose(
                    userPermissionsVersion: 7,
                    defaultUserAccessVersion: 3),
                actual: actual);
            Assert.Equal(
                expected: 0,
                actual: client.GetPermissionsVersionCallCount);
            Assert.Equal(
                expected: 0,
                actual: client.GetDefaultUserAccessVersionCallCount);
        }

        [Fact]
        public async Task GetCurrentAsync_WhenCacheMisses_LoadsIdentityAndWritesFreshAndStaleEntries()
        {
            var userId = Guid.Parse("6b5e05ae-3b4f-4717-ba15-b26f03c743a6");
            var cache = new RecordingDistributedCache();
            var client = new FakeIdentityInternalUsersClient
            {
                DefaultUserAccessVersion = 5
            };
            client.UserPermissionsVersions[userId] = 11;
            var store = new CachedPermissionsVersionStore(
                distributedCache: cache,
                client: client,
                options: CreatePermissionsVersionOptions(
                    cacheTtlSeconds: 120,
                    staleCacheTtlSeconds: 600,
                    allowStaleCacheOnIdentityFailure: true),
                logger: NullLogger<CachedPermissionsVersionStore>.Instance);

            int actual = await store.GetCurrentAsync(
                userId: userId,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: PermissionsVersionComposer.Compose(
                    userPermissionsVersion: 11,
                    defaultUserAccessVersion: 5),
                actual: actual);
            Assert.Equal(
                expected: 1,
                actual: client.GetPermissionsVersionCallCount);
            Assert.Equal(
                expected: 1,
                actual: client.GetDefaultUserAccessVersionCallCount);
            Assert.Equal(
                expected: "11",
                actual: cache.ReadString(AuthorizationCacheKeys.PermissionsVersion(userId)));
            Assert.Equal(
                expected: "11",
                actual: cache.ReadString(AuthorizationCacheKeys.PermissionsVersionStale(userId)));
            Assert.Equal(
                expected: "5",
                actual: cache.ReadString(AuthorizationCacheKeys.DefaultUserAccessVersion()));
            Assert.Equal(
                expected: "5",
                actual: cache.ReadString(AuthorizationCacheKeys.DefaultUserAccessVersionStale()));
            Assert.Equal(
                expected: TimeSpan.FromSeconds(120),
                actual: cache.WrittenOptions[AuthorizationCacheKeys.PermissionsVersion(userId)]
                   .AbsoluteExpirationRelativeToNow);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(600),
                actual: cache.WrittenOptions[AuthorizationCacheKeys.PermissionsVersionStale(userId)]
                   .AbsoluteExpirationRelativeToNow);
        }

        [Fact]
        public async Task GetCurrentAsync_WhenIdentityIsUnavailable_UsesStaleFallbacks()
        {
            var userId = Guid.Parse("5a0409b0-d1eb-4a20-9597-f963999a8014");
            var cache = new RecordingDistributedCache();
            var client = new FakeIdentityInternalUsersClient
            {
                GetPermissionsVersionException = new HttpRequestException("identity user version unavailable"),
                GetDefaultUserAccessVersionException = new HttpRequestException("identity default version unavailable")
            };
            cache.SeedString(
                key: AuthorizationCacheKeys.PermissionsVersionStale(userId),
                value: "13");
            cache.SeedString(
                key: AuthorizationCacheKeys.DefaultUserAccessVersionStale(),
                value: "6");
            var store = new CachedPermissionsVersionStore(
                distributedCache: cache,
                client: client,
                options: CreatePermissionsVersionOptions(allowStaleCacheOnIdentityFailure: true),
                logger: NullLogger<CachedPermissionsVersionStore>.Instance);

            int actual = await store.GetCurrentAsync(
                userId: userId,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: PermissionsVersionComposer.Compose(
                    userPermissionsVersion: 13,
                    defaultUserAccessVersion: 6),
                actual: actual);
            Assert.Equal(
                expected: 1,
                actual: client.GetPermissionsVersionCallCount);
            Assert.Equal(
                expected: 1,
                actual: client.GetDefaultUserAccessVersionCallCount);
        }

        [Fact]
        public async Task GetCurrentAsync_WhenIdentityFailsWithoutFallback_ThrowsUnavailableException()
        {
            var userId = Guid.Parse("5ee0833a-7cf0-4321-bcdf-9c4d30136edb");
            var cache = new RecordingDistributedCache();
            var client = new FakeIdentityInternalUsersClient
            {
                GetPermissionsVersionException = new HttpRequestException("identity unavailable")
            };
            var store = new CachedPermissionsVersionStore(
                distributedCache: cache,
                client: client,
                options: CreatePermissionsVersionOptions(allowStaleCacheOnIdentityFailure: true),
                logger: NullLogger<CachedPermissionsVersionStore>.Instance);

            PermissionsVersionUnavailableException exception =
                await Assert.ThrowsAsync<PermissionsVersionUnavailableException>(() => store.GetCurrentAsync(
                    userId: userId,
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: userId,
                actual: exception.UserId);
        }
    }
}
