using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class RedisClassicCitySetupSessionStore(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<ClassicCitySetupSessionOptions> options,
        TimeProvider timeProvider)
        : IClassicCitySetupSessionStore
    {
        private const string RecoveryIndexKey = "simulationcore:classic-city:setup-session:recovery";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer;
        private readonly IDistributedCache _distributedCache = distributedCache;
        private readonly ClassicCitySetupSessionOptions _options = options.Value;
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<ClassicCitySetupSessionState?> GetAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            string? payload = await _distributedCache.GetStringAsync(
                key: BuildCacheKey(sessionId),
                token: cancellationToken);

            return string.IsNullOrWhiteSpace(payload)
                ? null
                : JsonSerializer.Deserialize<ClassicCitySetupSessionState>(
                    json: payload,
                    options: JsonOptions);
        }

        public async Task<IReadOnlyList<ClassicCitySetupSessionState>> ListOwnedAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDatabase database = _connectionMultiplexer.GetDatabase();
            RedisValue[] values = await database.SetMembersAsync(BuildOwnerIndexKey(ownerUserId));

            if (values.Length == 0)
                return [];

            Guid[] sessionIds = values
               .Select(value => Guid.TryParse(
                    input: value,
                    result: out Guid sessionId)
                    ? sessionId
                    : Guid.Empty)
               .Where(sessionId => sessionId != Guid.Empty)
               .Distinct()
               .ToArray();

            if (sessionIds.Length == 0)
                return [];

            var staleIds = new List<Guid>();
            var sessions = new List<ClassicCitySetupSessionState>(sessionIds.Length);

            foreach (Guid sessionId in sessionIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ClassicCitySetupSessionState? session = await GetAsync(
                    sessionId: sessionId,
                    cancellationToken: cancellationToken);

                if (session is null)
                {
                    staleIds.Add(sessionId);
                    continue;
                }

                if (session.OwnerUserId != ownerUserId)
                    continue;

                sessions.Add(session);
            }

            if (staleIds.Count > 0)
            {
                RedisValue[] staleValues = staleIds
                   .Select(sessionId => (RedisValue)sessionId.ToString("D"))
                   .ToArray();

                await database.SetRemoveAsync(
                    key: BuildOwnerIndexKey(ownerUserId),
                    values: staleValues);
            }

            return sessions
               .OrderByDescending(session => session.UpdatedAtUtc)
               .ToArray();
        }

        public async Task DeleteAsync(
            Guid sessionId,
            Guid? ownerUserId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _distributedCache.RemoveAsync(
                key: BuildCacheKey(sessionId),
                token: cancellationToken);

            IDatabase database = _connectionMultiplexer.GetDatabase();
            string value = sessionId.ToString("D");

            if (ownerUserId.HasValue)
                await database.SetRemoveAsync(
                    key: BuildOwnerIndexKey(ownerUserId.Value),
                    value: value);

            await database.SetRemoveAsync(
                key: RecoveryIndexKey,
                value: value);
        }

        public Task SaveAsync(
            ClassicCitySetupSessionState session,
            CancellationToken cancellationToken = default)
        {
            string payload = JsonSerializer.Serialize(
                value: session,
                options: JsonOptions);

            return SaveCoreAsync(
                session: session,
                payload: payload,
                cancellationToken: cancellationToken);
        }

        public async Task<ClassicCitySetupSessionLockHandle?> TryAcquireLockAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await TryAcquireLockCoreAsync(
                lockKey: BuildLockKey(sessionId),
                cancellationToken: cancellationToken);
        }

        public async Task<ClassicCitySetupSessionLockHandle?> TryAcquireCreateLockAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default)
        {
            return await TryAcquireLockCoreAsync(
                lockKey: BuildCreateLockKey(ownerUserId),
                cancellationToken: cancellationToken);
        }

        public async Task ReleaseLockAsync(
            Guid sessionId,
            ClassicCitySetupSessionLockHandle lockHandle,
            CancellationToken cancellationToken = default)
        {
            await ReleaseLockCoreAsync(
                lockKey: BuildLockKey(sessionId),
                lockHandle: lockHandle,
                cancellationToken: cancellationToken);
        }

        public async Task ReleaseCreateLockAsync(
            Guid ownerUserId,
            ClassicCitySetupSessionLockHandle lockHandle,
            CancellationToken cancellationToken = default)
        {
            await ReleaseLockCoreAsync(
                lockKey: BuildCreateLockKey(ownerUserId),
                lockHandle: lockHandle,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> ListTrackedSessionIdsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDatabase database = _connectionMultiplexer.GetDatabase();
            RedisValue[] values = await database.SetMembersAsync(RecoveryIndexKey);

            return values
               .Select(value => Guid.TryParse(
                    input: value,
                    result: out Guid sessionId)
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
                options: BuildCacheEntryOptions(session.Status),
                token: cancellationToken);

            IDatabase database = _connectionMultiplexer.GetDatabase();
            string value = session.SessionId.ToString("D");

            if (session.OwnerUserId.HasValue)
                await database.SetAddAsync(
                    key: BuildOwnerIndexKey(session.OwnerUserId.Value),
                    value: value);

            if (ShouldTrackForRecovery(session.Status))
                await database.SetAddAsync(
                    key: RecoveryIndexKey,
                    value: value);
            else
                await database.SetRemoveAsync(
                    key: RecoveryIndexKey,
                    value: value);
        }

        private DistributedCacheEntryOptions BuildCacheEntryOptions(string status)
        {
            TimeSpan slidingExpiration = IsDraftStatus(status)
                ? TimeSpan.FromMinutes(_options.DraftTtlMinutes)
                : TimeSpan.FromHours(_options.CacheTtlHours);

            return new DistributedCacheEntryOptions
            {
                SlidingExpiration = slidingExpiration
            };
        }

        private static bool IsDraftStatus(string status)
        {
            return status is ClassicCitySetupSessionStatuses.Draft
             or ClassicCitySetupSessionStatuses.LaunchFailed;
        }

        private static bool ShouldTrackForRecovery(string status)
        {
            return status is ClassicCitySetupSessionStatuses.LaunchQueued
             or ClassicCitySetupSessionStatuses.CreatingCity
             or ClassicCitySetupSessionStatuses.BootstrappingPopulation
             or ClassicCitySetupSessionStatuses.ProvisioningFailed;
        }

        private async Task<ClassicCitySetupSessionLockHandle?> TryAcquireLockCoreAsync(
            string lockKey,
            CancellationToken cancellationToken)
        {
            IDatabase database = _connectionMultiplexer.GetDatabase();
            string token = Guid.NewGuid()
               .ToString("N");
            var lease = TimeSpan.FromSeconds(_options.MutationLockLeaseSeconds);
            DateTimeOffset deadline =
                _timeProvider.GetUtcNow()
                   .AddMilliseconds(_options.MutationLockAcquireTimeoutMilliseconds);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool acquired = await database.LockTakeAsync(
                    key: lockKey,
                    value: token,
                    expiry: lease);

                if (acquired)
                    return new ClassicCitySetupSessionLockHandle(token);

                if (_timeProvider.GetUtcNow() >= deadline)
                    return null;

                await Task.Delay(
                    millisecondsDelay: _options.MutationLockRetryDelayMilliseconds,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task ReleaseLockCoreAsync(
            string lockKey,
            ClassicCitySetupSessionLockHandle lockHandle,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDatabase database = _connectionMultiplexer.GetDatabase();
            await database.LockReleaseAsync(
                key: lockKey,
                value: lockHandle.Token);
        }

        private static string BuildCacheKey(Guid sessionId)
        {
            return $"simulationcore:classic-city:setup-session:{sessionId:D}";
        }

        private static string BuildLockKey(Guid sessionId)
        {
            return $"simulationcore:classic-city:setup-session-lock:{sessionId:D}";
        }

        private static string BuildOwnerIndexKey(Guid ownerUserId)
        {
            return $"simulationcore:classic-city:setup-session:owner:{ownerUserId:D}";
        }

        private static string BuildCreateLockKey(Guid ownerUserId)
        {
            return $"simulationcore:classic-city:setup-session:create-lock:{ownerUserId:D}";
        }
    }
}
