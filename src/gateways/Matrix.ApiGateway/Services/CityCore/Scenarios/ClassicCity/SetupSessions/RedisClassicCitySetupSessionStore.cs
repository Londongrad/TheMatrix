using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Matrix.ApiGateway.Configurations.Options;
using StackExchange.Redis;

namespace Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class RedisClassicCitySetupSessionStore(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<ClassicCitySetupSessionOptions> options)
        : IClassicCitySetupSessionStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private const string RecoveryIndexKey = "citycore:classic-city:setup-session:recovery";
        private readonly IDistributedCache _distributedCache = distributedCache;
        private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer;
        private readonly ClassicCitySetupSessionOptions _options = options.Value;

        public async Task<ClassicCitySetupSessionState?> GetAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            string? payload = await _distributedCache.GetStringAsync(
                key: BuildCacheKey(sessionId),
                token: cancellationToken);

            return string.IsNullOrWhiteSpace(payload)
                ? null
                : JsonSerializer.Deserialize<ClassicCitySetupSessionState>(payload, JsonOptions);
        }

        public Task SaveAsync(
            ClassicCitySetupSessionState session,
            CancellationToken cancellationToken = default)
        {
            string payload = JsonSerializer.Serialize(session, JsonOptions);

            return SaveCoreAsync(
                session: session,
                payload: payload,
                cancellationToken: cancellationToken);
        }

        public async Task<ClassicCitySetupSessionLockHandle?> TryAcquireLockAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            IDatabase database = _connectionMultiplexer.GetDatabase();
            string lockKey = BuildLockKey(sessionId);
            string token = Guid.NewGuid()
               .ToString("N");
            TimeSpan lease = TimeSpan.FromSeconds(_options.MutationLockLeaseSeconds);
            DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMilliseconds(_options.MutationLockAcquireTimeoutMilliseconds);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool acquired = await database.LockTakeAsync(
                    key: lockKey,
                    value: token,
                    expiry: lease);

                if (acquired)
                    return new ClassicCitySetupSessionLockHandle(token);

                if (DateTimeOffset.UtcNow >= deadline)
                    return null;

                await Task.Delay(
                    millisecondsDelay: _options.MutationLockRetryDelayMilliseconds,
                    cancellationToken: cancellationToken);
            }
        }

        public async Task ReleaseLockAsync(
            Guid sessionId,
            ClassicCitySetupSessionLockHandle lockHandle,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDatabase database = _connectionMultiplexer.GetDatabase();
            await database.LockReleaseAsync(
                key: BuildLockKey(sessionId),
                value: lockHandle.Token);
        }

        public async Task<IReadOnlyList<Guid>> ListTrackedSessionIdsAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDatabase database = _connectionMultiplexer.GetDatabase();
            RedisValue[] values = await database.SetMembersAsync(RecoveryIndexKey);

            return values
               .Select(value => Guid.TryParse(value, out Guid sessionId)
                    ? sessionId
                    : Guid.Empty)
               .Where(sessionId => sessionId != Guid.Empty)
               .ToArray();
        }

        public async Task UntrackAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDatabase database = _connectionMultiplexer.GetDatabase();
            await database.SetRemoveAsync(
                key: RecoveryIndexKey,
                value: sessionId.ToString("D"));
        }

        private async Task SaveCoreAsync(
            ClassicCitySetupSessionState session,
            string payload,
            CancellationToken cancellationToken)
        {
            await _distributedCache.SetStringAsync(
                key: BuildCacheKey(session.SessionId),
                value: payload,
                options: new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(_options.CacheTtlHours)
                },
                token: cancellationToken);

            IDatabase database = _connectionMultiplexer.GetDatabase();
            string value = session.SessionId.ToString("D");

            if (ShouldTrackForRecovery(session.Status))
                await database.SetAddAsync(
                    key: RecoveryIndexKey,
                    value: value);
            else
                await database.SetRemoveAsync(
                    key: RecoveryIndexKey,
                    value: value);
        }

        private static bool ShouldTrackForRecovery(string status)
        {
            return status is ClassicCitySetupSessionStatuses.LaunchQueued or
                ClassicCitySetupSessionStatuses.CreatingCity or
                ClassicCitySetupSessionStatuses.BootstrappingPopulation or
                ClassicCitySetupSessionStatuses.ProvisioningFailed;
        }

        private static string BuildCacheKey(Guid sessionId)
        {
            return $"citycore:classic-city:setup-session:{sessionId:D}";
        }

        private static string BuildLockKey(Guid sessionId)
        {
            return $"citycore:classic-city:setup-session-lock:{sessionId:D}";
        }
    }
}
