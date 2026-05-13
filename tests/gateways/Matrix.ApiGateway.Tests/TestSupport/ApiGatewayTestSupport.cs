using System.Text;
using Matrix.ApiGateway.Authorization.AuthContext.Options;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Tests.TestSupport;

public static class ApiGatewayTestSupport
{
    private const string CurrentSigningKey = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&";
    private const string NextSigningKey = "Z9y8X7w6V5u4T3s2R1q0P)o(I*u&Y^t%R$";

    public static IOptions<AuthContextOptions> CreateAuthContextOptions(int cacheTtlSeconds = 1800)
    {
        return Options.Create(new AuthContextOptions
        {
            CacheTtlSeconds = cacheTtlSeconds
        });
    }

    public static IOptions<PermissionsVersionOptions> CreatePermissionsVersionOptions(
        int cacheTtlSeconds = 300,
        int staleCacheTtlSeconds = 21600,
        bool allowStaleCacheOnIdentityFailure = true)
    {
        return Options.Create(new PermissionsVersionOptions
        {
            CacheTtlSeconds = cacheTtlSeconds,
            StaleCacheTtlSeconds = staleCacheTtlSeconds,
            AllowStaleCacheOnIdentityFailure = allowStaleCacheOnIdentityFailure
        });
    }

    public static IOptions<InternalUserContextJwtOptions> CreateInternalJwtOptions(int lifetimeSeconds = 60)
    {
        return Options.Create(new InternalUserContextJwtOptions
        {
            Issuer = "matrix-gateway",
            Audience = "matrix-internal",
            LifetimeSeconds = lifetimeSeconds,
            CurrentKeyId = "kid-current",
            SigningKey = CurrentSigningKey,
            Keys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["kid-current"] = CurrentSigningKey,
                ["kid-next"] = NextSigningKey
            }
        });
    }

    public static TimeProvider CreateTimeProvider(DateTimeOffset utcNow)
    {
        return new FrozenTimeProvider(utcNow);
    }

    public sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    public sealed class FakeIdentityInternalUsersClient : IIdentityInternalUsersClient
    {
        public Dictionary<Guid, int> UserPermissionsVersions { get; } = new();
        public Dictionary<Guid, UserAuthContextResponse> UserAuthContexts { get; } = new();
        public int DefaultUserAccessVersion { get; set; } = 1;
        public Exception? GetPermissionsVersionException { get; set; }
        public Exception? GetDefaultUserAccessVersionException { get; set; }
        public Exception? GetAuthContextException { get; set; }
        public int GetPermissionsVersionCallCount { get; private set; }
        public int GetDefaultUserAccessVersionCallCount { get; private set; }
        public int GetAuthContextCallCount { get; private set; }

        public Task<int> GetPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken)
        {
            GetPermissionsVersionCallCount++;

            if (GetPermissionsVersionException is not null)
                throw GetPermissionsVersionException;

            if (!UserPermissionsVersions.TryGetValue(userId, out int version))
                throw new KeyNotFoundException($"Permissions version for user '{userId}' was not configured.");

            return Task.FromResult(version);
        }

        public Task<int> GetDefaultUserAccessVersionAsync(CancellationToken cancellationToken)
        {
            GetDefaultUserAccessVersionCallCount++;

            if (GetDefaultUserAccessVersionException is not null)
                throw GetDefaultUserAccessVersionException;

            return Task.FromResult(DefaultUserAccessVersion);
        }

        public Task<UserAuthContextResponse> GetAuthContextAsync(Guid userId, CancellationToken cancellationToken)
        {
            GetAuthContextCallCount++;

            if (GetAuthContextException is not null)
                throw GetAuthContextException;

            if (!UserAuthContexts.TryGetValue(userId, out UserAuthContextResponse? context))
                throw new KeyNotFoundException($"Auth context for user '{userId}' was not configured.");

            return Task.FromResult(context);
        }
    }

    public sealed class RecordingDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

        public Dictionary<string, DistributedCacheEntryOptions> WrittenOptions { get; } = new(StringComparer.Ordinal);
        public Exception? GetException { get; set; }
        public Exception? SetException { get; set; }

        public byte[]? Get(string key)
        {
            return _entries.TryGetValue(key, out byte[]? value)
                ? value.ToArray()
                : null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            if (GetException is not null)
                throw GetException;

            return Task.FromResult(Get(key));
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _entries.Remove(key);
            WrittenOptions.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _entries[key] = value.ToArray();
            WrittenOptions[key] = CloneOptions(options);
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            if (SetException is not null)
                throw SetException;

            Set(
                key: key,
                value: value,
                options: options);

            return Task.CompletedTask;
        }

        public void SeedString(string key, string value)
        {
            _entries[key] = Encoding.UTF8.GetBytes(value);
        }

        public string? ReadString(string key)
        {
            return _entries.TryGetValue(key, out byte[]? value)
                ? Encoding.UTF8.GetString(value)
                : null;
        }

        private static DistributedCacheEntryOptions CloneOptions(DistributedCacheEntryOptions options)
        {
            return new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = options.AbsoluteExpiration,
                AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = options.SlidingExpiration
            };
        }
    }
}
