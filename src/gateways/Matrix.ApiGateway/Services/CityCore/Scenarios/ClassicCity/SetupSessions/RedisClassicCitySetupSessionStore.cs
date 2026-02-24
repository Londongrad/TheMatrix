using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Matrix.ApiGateway.Configurations.Options;

namespace Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class RedisClassicCitySetupSessionStore(
        IDistributedCache distributedCache,
        IOptions<ClassicCitySetupSessionOptions> options)
        : IClassicCitySetupSessionStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IDistributedCache _distributedCache = distributedCache;
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

            return _distributedCache.SetStringAsync(
                key: BuildCacheKey(session.SessionId),
                value: payload,
                options: new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(_options.CacheTtlHours)
                },
                token: cancellationToken);
        }

        private static string BuildCacheKey(Guid sessionId)
        {
            return $"citycore:classic-city:setup-session:{sessionId:D}";
        }
    }
}
