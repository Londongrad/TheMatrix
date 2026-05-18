using System.Text.Json;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Tests.TestSupport;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;

public sealed class RedisClassicCitySetupSessionStoreTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetAsync_WhenCacheEntryIsMissing_ReturnsNull()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var redis = new FakeRedisDatabaseState();
        var store = CreateStore(cache, redis);

        ClassicCitySetupSessionState? session = await store.GetAsync(Guid.NewGuid());

        Assert.Null(session);
    }

    [Fact]
    public async Task GetAsync_WhenCacheEntryExists_DeserializesSession()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var redis = new FakeRedisDatabaseState();
        var store = CreateStore(cache, redis);
        Guid sessionId = Guid.Parse("b5c32d53-59fa-4d58-a930-4b979e00b2e1");
        ClassicCitySetupSessionState expected = ApiGatewayTestSupport.CreateClassicCitySetupSessionState(
            sessionId: sessionId,
            ownerUserId: Guid.Parse("d60845ab-79f4-4627-b3d0-9b65548e4f8b"),
            status: ClassicCitySetupSessionStatuses.LaunchQueued,
            updatedAtUtc: new DateTimeOffset(2048, 6, 2, 9, 30, 0, TimeSpan.Zero));
        cache.SeedString(BuildCacheKey(sessionId), JsonSerializer.Serialize(expected, JsonOptions));

        ClassicCitySetupSessionState? actual = await store.GetAsync(sessionId);

        Assert.NotNull(actual);
        Assert.Equal(expected.SessionId, actual!.SessionId);
        Assert.Equal(expected.OwnerUserId, actual.OwnerUserId);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.UpdatedAtUtc, actual.UpdatedAtUtc);
    }

    [Fact]
    public async Task SaveAsync_WhenSessionIsDraft_WritesDraftTtlAndOwnerIndexWithoutRecoveryTracking()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var redis = new FakeRedisDatabaseState();
        var store = CreateStore(
            cache,
            redis,
            Options.Create(new ClassicCitySetupSessionOptions
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
        Assert.Equal(TimeSpan.FromMinutes(15), cache.WrittenOptions[cacheKey].SlidingExpiration);
        Assert.True(redis.SetContains(BuildOwnerIndexKey(session.OwnerUserId!.Value), session.SessionId.ToString("D")));
        Assert.False(redis.SetContains(RecoveryIndexKey, session.SessionId.ToString("D")));
    }

    [Fact]
    public async Task SaveAsync_WhenSessionRequiresRecovery_TracksSessionAndUsesLongCacheTtl()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var redis = new FakeRedisDatabaseState();
        var store = CreateStore(
            cache,
            redis,
            Options.Create(new ClassicCitySetupSessionOptions
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
        Assert.Equal(TimeSpan.FromHours(36), cache.WrittenOptions[cacheKey].SlidingExpiration);
        Assert.True(redis.SetContains(BuildOwnerIndexKey(session.OwnerUserId!.Value), session.SessionId.ToString("D")));
        Assert.True(redis.SetContains(RecoveryIndexKey, session.SessionId.ToString("D")));
    }

    [Fact]
    public async Task ListOwnedAsync_WhenIndexContainsOwnedForeignAndStaleEntries_ReturnsOwnedSessionsOrderedAndRemovesStaleIds()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var redis = new FakeRedisDatabaseState();
        var store = CreateStore(cache, redis);
        Guid ownerUserId = Guid.Parse("4df92718-1d0c-49ca-9345-96fc4f26bd45");
        Guid ownedOlderId = Guid.Parse("a4ec0274-a878-47ff-aa72-53fd3f4ce93d");
        Guid ownedNewerId = Guid.Parse("56d5f2d4-d834-45f7-8a93-670695ea4428");
        Guid foreignId = Guid.Parse("553b562c-868f-4f8b-b860-f4462f88bb4e");
        Guid staleId = Guid.Parse("328e86fe-fc92-468d-8b07-10c7f2914cb2");

        redis.SeedSet(
            BuildOwnerIndexKey(ownerUserId),
            ownedOlderId.ToString("D"),
            ownedNewerId.ToString("D"),
            foreignId.ToString("D"),
            staleId.ToString("D"),
            "not-a-guid");

        cache.SeedString(
            BuildCacheKey(ownedOlderId),
            JsonSerializer.Serialize(
                ApiGatewayTestSupport.CreateClassicCitySetupSessionState(
                    sessionId: ownedOlderId,
                    ownerUserId: ownerUserId,
                    updatedAtUtc: new DateTimeOffset(2048, 6, 2, 8, 0, 0, TimeSpan.Zero)),
                JsonOptions));
        cache.SeedString(
            BuildCacheKey(ownedNewerId),
            JsonSerializer.Serialize(
                ApiGatewayTestSupport.CreateClassicCitySetupSessionState(
                    sessionId: ownedNewerId,
                    ownerUserId: ownerUserId,
                    updatedAtUtc: new DateTimeOffset(2048, 6, 2, 10, 0, 0, TimeSpan.Zero)),
                JsonOptions));
        cache.SeedString(
            BuildCacheKey(foreignId),
            JsonSerializer.Serialize(
                ApiGatewayTestSupport.CreateClassicCitySetupSessionState(
                    sessionId: foreignId,
                    ownerUserId: Guid.Parse("136f3ddd-b276-44af-b615-0987c57ae236"),
                    updatedAtUtc: new DateTimeOffset(2048, 6, 2, 11, 0, 0, TimeSpan.Zero)),
                JsonOptions));

        IReadOnlyList<ClassicCitySetupSessionState> sessions = await store.ListOwnedAsync(ownerUserId);

        Assert.Collection(
            sessions,
            session => Assert.Equal(ownedNewerId, session.SessionId),
            session => Assert.Equal(ownedOlderId, session.SessionId));
        Assert.False(redis.SetContains(BuildOwnerIndexKey(ownerUserId), staleId.ToString("D")));
        Assert.True(redis.SetContains(BuildOwnerIndexKey(ownerUserId), foreignId.ToString("D")));
    }

    [Fact]
    public async Task DeleteAsync_WhenOwnerIsProvided_RemovesCacheOwnerAndRecoveryEntries()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var redis = new FakeRedisDatabaseState();
        var store = CreateStore(cache, redis);
        Guid ownerUserId = Guid.Parse("24d175bb-b4ae-4550-b5d2-eeaeb4af4f69");
        Guid sessionId = Guid.Parse("fb58ba10-a20a-45a5-bc54-0af60d3d2941");
        cache.SeedString(BuildCacheKey(sessionId), "{}");
        redis.SeedSet(BuildOwnerIndexKey(ownerUserId), sessionId.ToString("D"));
        redis.SeedSet(RecoveryIndexKey, sessionId.ToString("D"));

        await store.DeleteAsync(sessionId, ownerUserId);

        Assert.Null(cache.ReadString(BuildCacheKey(sessionId)));
        Assert.False(redis.SetContains(BuildOwnerIndexKey(ownerUserId), sessionId.ToString("D")));
        Assert.False(redis.SetContains(RecoveryIndexKey, sessionId.ToString("D")));
    }

    [Fact]
    public async Task ListTrackedSessionIdsAsync_WhenRecoveryIndexContainsInvalidIds_ReturnsOnlyValidSessionIds()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var redis = new FakeRedisDatabaseState();
        var store = CreateStore(cache, redis);
        Guid trackedOne = Guid.Parse("fb9c81d9-ef7f-4be0-bf58-23d836b472ec");
        Guid trackedTwo = Guid.Parse("2ef81eb2-6f7a-4e7b-a0d0-0c24b6674c6f");
        redis.SeedSet(RecoveryIndexKey, trackedOne.ToString("D"), "oops", trackedTwo.ToString("D"));

        IReadOnlyList<Guid> tracked = await store.ListTrackedSessionIdsAsync();

        Assert.Equal([trackedOne, trackedTwo], tracked);
    }

    [Fact]
    public async Task UntrackAsync_RemovesSessionFromRecoveryIndex()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var redis = new FakeRedisDatabaseState();
        var store = CreateStore(cache, redis);
        Guid sessionId = Guid.Parse("7dbf2973-bcac-4386-b34f-d925cf6a414f");
        redis.SeedSet(RecoveryIndexKey, sessionId.ToString("D"));

        await store.UntrackAsync(sessionId);

        Assert.False(redis.SetContains(RecoveryIndexKey, sessionId.ToString("D")));
    }

    [Fact]
    public async Task TryAcquireLockAsync_AndReleaseLock_UseRedisLockState()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var redis = new FakeRedisDatabaseState();
        var store = CreateStore(cache, redis);
        Guid sessionId = Guid.Parse("c95eb312-b064-40f4-a66b-0fba9cb9d587");
        string lockKey = BuildLockKey(sessionId);

        ClassicCitySetupSessionLockHandle? handle = await store.TryAcquireLockAsync(sessionId);

        Assert.NotNull(handle);
        Assert.True(redis.HasLock(lockKey));
        await store.ReleaseLockAsync(sessionId, handle!);
        Assert.False(redis.HasLock(lockKey));
        Assert.Contains(redis.ReleasedLocks, item => item.Key == lockKey && item.Token == handle!.Token);
    }

    [Fact]
    public async Task TryAcquireCreateLockAsync_WhenLockRemainsHeldUntilTimeout_ReturnsNull()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var redis = new FakeRedisDatabaseState();
        Guid ownerUserId = Guid.Parse("52bc91c5-acf5-4a77-a51e-f8e5ea537546");
        string lockKey = BuildCreateLockKey(ownerUserId);
        redis.SeedLock(lockKey, "existing-lock");
        var store = CreateStore(
            cache,
            redis,
            Options.Create(new ClassicCitySetupSessionOptions
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
        IOptions<ClassicCitySetupSessionOptions>? options = null)
    {
        cache ??= new ApiGatewayTestSupport.RecordingDistributedCache();
        redis ??= new FakeRedisDatabaseState();
        options ??= Options.Create(new ClassicCitySetupSessionOptions
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
            options: options);
    }

    private static string BuildCacheKey(Guid sessionId) =>
        $"simulationcore:classic-city:setup-session:{sessionId:D}";

    private static string BuildOwnerIndexKey(Guid ownerUserId) =>
        $"simulationcore:classic-city:setup-session:owner:{ownerUserId:D}";

    private static string BuildLockKey(Guid sessionId) =>
        $"simulationcore:classic-city:setup-session-lock:{sessionId:D}";

    private static string BuildCreateLockKey(Guid ownerUserId) =>
        $"simulationcore:classic-city:setup-session:create-lock:{ownerUserId:D}";

    private const string RecoveryIndexKey = "simulationcore:classic-city:setup-session:recovery";
}
