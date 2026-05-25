using System.Text.Json;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Tests.TestSupport;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class RedisClassicCitySetupSessionStoreTests
    {
        private const string RecoveryIndexKey = "simulationcore:classic-city:setup-session:recovery";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        [Fact]
        public async Task GetAsync_WhenCacheEntryIsMissing_ReturnsNull()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var redis = new FakeRedisDatabaseState();
            RedisClassicCitySetupSessionStore store = CreateStore(
                cache: cache,
                redis: redis);

            ClassicCitySetupSessionState? session = await store.GetAsync(Guid.NewGuid());

            Assert.Null(session);
        }

        [Fact]
        public async Task GetAsync_WhenCacheEntryExists_DeserializesSession()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var redis = new FakeRedisDatabaseState();
            RedisClassicCitySetupSessionStore store = CreateStore(
                cache: cache,
                redis: redis);
            var sessionId = Guid.Parse("b5c32d53-59fa-4d58-a930-4b979e00b2e1");
            ClassicCitySetupSessionState expected = ApiGatewayTestSupport.CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: Guid.Parse("d60845ab-79f4-4627-b3d0-9b65548e4f8b"),
                status: ClassicCitySetupSessionStatuses.LaunchQueued,
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 2,
                    hour: 9,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero));
            cache.SeedString(
                key: BuildCacheKey(sessionId),
                value: JsonSerializer.Serialize(
                    value: expected,
                    options: JsonOptions));

            ClassicCitySetupSessionState? actual = await store.GetAsync(sessionId);

            Assert.NotNull(actual);
            Assert.Equal(
                expected: expected.SessionId,
                actual: actual!.SessionId);
            Assert.Equal(
                expected: expected.OwnerUserId,
                actual: actual.OwnerUserId);
            Assert.Equal(
                expected: expected.Status,
                actual: actual.Status);
            Assert.Equal(
                expected: expected.UpdatedAtUtc,
                actual: actual.UpdatedAtUtc);
        }

        [Fact]
        public async Task SaveAsync_WhenSessionIsDraft_WritesDraftTtlAndOwnerIndexWithoutRecoveryTracking()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var redis = new FakeRedisDatabaseState();
            RedisClassicCitySetupSessionStore store = CreateStore(
                cache: cache,
                redis: redis,
                options: Options.Create(
                    new ClassicCitySetupSessionOptions
                    {
                        CacheTtlHours = 72,
                        DraftTtlMinutes = 15
                    }));
            ClassicCitySetupSessionState session = ApiGatewayTestSupport.CreateClassicCitySetupSessionState(
                sessionId: Guid.Parse("71b76f42-e4a7-4f0c-8ee4-f399efe5f25a"),
                ownerUserId: Guid.Parse("e7079761-85d8-4fbd-abd4-3d924bcd4805"),
                status: ClassicCitySetupSessionStatuses.Draft);

            await store.SaveAsync(session);

            string cacheKey = BuildCacheKey(session.SessionId);
            Assert.NotNull(cache.ReadString(cacheKey));
            Assert.Equal(
                expected: TimeSpan.FromMinutes(15),
                actual: cache.WrittenOptions[cacheKey].SlidingExpiration);
            Assert.True(
                redis.SetContains(
                    key: BuildOwnerIndexKey(session.OwnerUserId!.Value),
                    value: session.SessionId.ToString("D")));
            Assert.False(
                redis.SetContains(
                    key: RecoveryIndexKey,
                    value: session.SessionId.ToString("D")));
        }

        [Fact]
        public async Task SaveAsync_WhenSessionRequiresRecovery_TracksSessionAndUsesLongCacheTtl()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var redis = new FakeRedisDatabaseState();
            RedisClassicCitySetupSessionStore store = CreateStore(
                cache: cache,
                redis: redis,
                options: Options.Create(
                    new ClassicCitySetupSessionOptions
                    {
                        CacheTtlHours = 36,
                        DraftTtlMinutes = 30
                    }));
            ClassicCitySetupSessionState session = ApiGatewayTestSupport.CreateClassicCitySetupSessionState(
                sessionId: Guid.Parse("55f4f89a-afcb-46d0-8770-03157eae508f"),
                ownerUserId: Guid.Parse("8f5f8e21-e7cf-4d48-afd1-6e2746e4f8b0"),
                status: ClassicCitySetupSessionStatuses.BootstrappingPopulation);

            await store.SaveAsync(session);

            string cacheKey = BuildCacheKey(session.SessionId);
            Assert.Equal(
                expected: TimeSpan.FromHours(36),
                actual: cache.WrittenOptions[cacheKey].SlidingExpiration);
            Assert.True(
                redis.SetContains(
                    key: BuildOwnerIndexKey(session.OwnerUserId!.Value),
                    value: session.SessionId.ToString("D")));
            Assert.True(
                redis.SetContains(
                    key: RecoveryIndexKey,
                    value: session.SessionId.ToString("D")));
        }

        [Fact]
        public async Task
            ListOwnedAsync_WhenIndexContainsOwnedForeignAndStaleEntries_ReturnsOwnedSessionsOrderedAndRemovesStaleIds()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var redis = new FakeRedisDatabaseState();
            RedisClassicCitySetupSessionStore store = CreateStore(
                cache: cache,
                redis: redis);
            var ownerUserId = Guid.Parse("4df92718-1d0c-49ca-9345-96fc4f26bd45");
            var ownedOlderId = Guid.Parse("a4ec0274-a878-47ff-aa72-53fd3f4ce93d");
            var ownedNewerId = Guid.Parse("56d5f2d4-d834-45f7-8a93-670695ea4428");
            var foreignId = Guid.Parse("553b562c-868f-4f8b-b860-f4462f88bb4e");
            var staleId = Guid.Parse("328e86fe-fc92-468d-8b07-10c7f2914cb2");

            redis.SeedSet(
                key: BuildOwnerIndexKey(ownerUserId),
                ownedOlderId.ToString("D"),
                ownedNewerId.ToString("D"),
                foreignId.ToString("D"),
                staleId.ToString("D"),
                "not-a-guid");

            cache.SeedString(
                key: BuildCacheKey(ownedOlderId),
                value: JsonSerializer.Serialize(
                    value: ApiGatewayTestSupport.CreateClassicCitySetupSessionState(
                        sessionId: ownedOlderId,
                        ownerUserId: ownerUserId,
                        updatedAtUtc: new DateTimeOffset(
                            year: 2048,
                            month: 6,
                            day: 2,
                            hour: 8,
                            minute: 0,
                            second: 0,
                            offset: TimeSpan.Zero)),
                    options: JsonOptions));
            cache.SeedString(
                key: BuildCacheKey(ownedNewerId),
                value: JsonSerializer.Serialize(
                    value: ApiGatewayTestSupport.CreateClassicCitySetupSessionState(
                        sessionId: ownedNewerId,
                        ownerUserId: ownerUserId,
                        updatedAtUtc: new DateTimeOffset(
                            year: 2048,
                            month: 6,
                            day: 2,
                            hour: 10,
                            minute: 0,
                            second: 0,
                            offset: TimeSpan.Zero)),
                    options: JsonOptions));
            cache.SeedString(
                key: BuildCacheKey(foreignId),
                value: JsonSerializer.Serialize(
                    value: ApiGatewayTestSupport.CreateClassicCitySetupSessionState(
                        sessionId: foreignId,
                        ownerUserId: Guid.Parse("136f3ddd-b276-44af-b615-0987c57ae236"),
                        updatedAtUtc: new DateTimeOffset(
                            year: 2048,
                            month: 6,
                            day: 2,
                            hour: 11,
                            minute: 0,
                            second: 0,
                            offset: TimeSpan.Zero)),
                    options: JsonOptions));

            IReadOnlyList<ClassicCitySetupSessionState> sessions = await store.ListOwnedAsync(ownerUserId);

            Assert.Collection(
                collection: sessions,
                session => Assert.Equal(
                    expected: ownedNewerId,
                    actual: session.SessionId),
                session => Assert.Equal(
                    expected: ownedOlderId,
                    actual: session.SessionId));
            Assert.False(
                redis.SetContains(
                    key: BuildOwnerIndexKey(ownerUserId),
                    value: staleId.ToString("D")));
            Assert.True(
                redis.SetContains(
                    key: BuildOwnerIndexKey(ownerUserId),
                    value: foreignId.ToString("D")));
        }

        [Fact]
        public async Task DeleteAsync_WhenOwnerIsProvided_RemovesCacheOwnerAndRecoveryEntries()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var redis = new FakeRedisDatabaseState();
            RedisClassicCitySetupSessionStore store = CreateStore(
                cache: cache,
                redis: redis);
            var ownerUserId = Guid.Parse("24d175bb-b4ae-4550-b5d2-eeaeb4af4f69");
            var sessionId = Guid.Parse("fb58ba10-a20a-45a5-bc54-0af60d3d2941");
            cache.SeedString(
                key: BuildCacheKey(sessionId),
                value: "{}");
            redis.SeedSet(
                key: BuildOwnerIndexKey(ownerUserId),
                sessionId.ToString("D"));
            redis.SeedSet(
                key: RecoveryIndexKey,
                sessionId.ToString("D"));

            await store.DeleteAsync(
                sessionId: sessionId,
                ownerUserId: ownerUserId);

            Assert.Null(cache.ReadString(BuildCacheKey(sessionId)));
            Assert.False(
                redis.SetContains(
                    key: BuildOwnerIndexKey(ownerUserId),
                    value: sessionId.ToString("D")));
            Assert.False(
                redis.SetContains(
                    key: RecoveryIndexKey,
                    value: sessionId.ToString("D")));
        }

        [Fact]
        public async Task ListTrackedSessionIdsAsync_WhenRecoveryIndexContainsInvalidIds_ReturnsOnlyValidSessionIds()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var redis = new FakeRedisDatabaseState();
            RedisClassicCitySetupSessionStore store = CreateStore(
                cache: cache,
                redis: redis);
            var trackedOne = Guid.Parse("fb9c81d9-ef7f-4be0-bf58-23d836b472ec");
            var trackedTwo = Guid.Parse("2ef81eb2-6f7a-4e7b-a0d0-0c24b6674c6f");
            redis.SeedSet(
                key: RecoveryIndexKey,
                trackedOne.ToString("D"),
                "oops",
                trackedTwo.ToString("D"));

            IReadOnlyList<Guid> tracked = await store.ListTrackedSessionIdsAsync();

            Assert.Equal(
                expected:
                [
                    trackedOne,
                    trackedTwo
                ],
                actual: tracked);
        }

        [Fact]
        public async Task UntrackAsync_RemovesSessionFromRecoveryIndex()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var redis = new FakeRedisDatabaseState();
            RedisClassicCitySetupSessionStore store = CreateStore(
                cache: cache,
                redis: redis);
            var sessionId = Guid.Parse("7dbf2973-bcac-4386-b34f-d925cf6a414f");
            redis.SeedSet(
                key: RecoveryIndexKey,
                sessionId.ToString("D"));

            await store.UntrackAsync(sessionId);

            Assert.False(
                redis.SetContains(
                    key: RecoveryIndexKey,
                    value: sessionId.ToString("D")));
        }

        [Fact]
        public async Task TryAcquireLockAsync_AndReleaseLock_UseRedisLockState()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var redis = new FakeRedisDatabaseState();
            RedisClassicCitySetupSessionStore store = CreateStore(
                cache: cache,
                redis: redis);
            var sessionId = Guid.Parse("c95eb312-b064-40f4-a66b-0fba9cb9d587");
            string lockKey = BuildLockKey(sessionId);

            ClassicCitySetupSessionLockHandle? handle = await store.TryAcquireLockAsync(sessionId);

            Assert.NotNull(handle);
            Assert.True(redis.HasLock(lockKey));
            await store.ReleaseLockAsync(
                sessionId: sessionId,
                lockHandle: handle!);
            Assert.False(redis.HasLock(lockKey));
            Assert.Contains(
                collection: redis.ReleasedLocks,
                filter: item => item.Key == lockKey && item.Token == handle!.Token);
        }

        [Fact]
        public async Task TryAcquireCreateLockAsync_WhenLockRemainsHeldUntilTimeout_ReturnsNull()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var redis = new FakeRedisDatabaseState();
            var ownerUserId = Guid.Parse("52bc91c5-acf5-4a77-a51e-f8e5ea537546");
            string lockKey = BuildCreateLockKey(ownerUserId);
            redis.SeedLock(
                key: lockKey,
                token: "existing-lock");
            RedisClassicCitySetupSessionStore store = CreateStore(
                cache: cache,
                redis: redis,
                options: Options.Create(
                    new ClassicCitySetupSessionOptions
                    {
                        MutationLockLeaseSeconds = 30,
                        MutationLockAcquireTimeoutMilliseconds = 0,
                        MutationLockRetryDelayMilliseconds = 1
                    }));

            ClassicCitySetupSessionLockHandle? handle = await store.TryAcquireCreateLockAsync(ownerUserId);

            Assert.Null(handle);
            Assert.True(redis.HasLock(lockKey));
            Assert.NotEmpty(redis.LockTakeKeys);
        }

        private static RedisClassicCitySetupSessionStore CreateStore(
            ApiGatewayTestSupport.RecordingDistributedCache? cache = null,
            FakeRedisDatabaseState? redis = null,
            IOptions<ClassicCitySetupSessionOptions>? options = null,
            TimeProvider? timeProvider = null)
        {
            cache ??= new ApiGatewayTestSupport.RecordingDistributedCache();
            redis ??= new FakeRedisDatabaseState();
            options ??= Options.Create(
                new ClassicCitySetupSessionOptions
                {
                    CacheTtlHours = 168,
                    DraftTtlMinutes = 60,
                    MutationLockLeaseSeconds = 900,
                    MutationLockAcquireTimeoutMilliseconds = 1500,
                    MutationLockRetryDelayMilliseconds = 100
                });

            return new RedisClassicCitySetupSessionStore(
                distributedCache: cache,
                connectionMultiplexer: RedisTestDoubles.CreateConnectionMultiplexer(redis),
                options: options,
                timeProvider: timeProvider ?? TimeProvider.System);
        }

        private static string BuildCacheKey(Guid sessionId)
        {
            return $"simulationcore:classic-city:setup-session:{sessionId:D}";
        }

        private static string BuildOwnerIndexKey(Guid ownerUserId)
        {
            return $"simulationcore:classic-city:setup-session:owner:{ownerUserId:D}";
        }

        private static string BuildLockKey(Guid sessionId)
        {
            return $"simulationcore:classic-city:setup-session-lock:{sessionId:D}";
        }

        private static string BuildCreateLockKey(Guid ownerUserId)
        {
            return $"simulationcore:classic-city:setup-session:create-lock:{ownerUserId:D}";
        }
    }
}
