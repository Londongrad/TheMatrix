using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MassTransit;
using System.Text;
using Matrix.ApiGateway.Authorization.AuthContext.Abstractions;
using Matrix.ApiGateway.Authorization.AuthContext.Options;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

    public static IOptions<ClassicCitySetupSessionOptions> CreateClassicCitySetupSessionOptions(
        int recentDraftReuseWindowSeconds = 30,
        int launchQueueRecoveryDelaySeconds = 20)
    {
        return Options.Create(new ClassicCitySetupSessionOptions
        {
            RecentDraftReuseWindowSeconds = recentDraftReuseWindowSeconds,
            LaunchQueueRecoveryDelaySeconds = launchQueueRecoveryDelaySeconds
        });
    }

    public static TimeProvider CreateTimeProvider(DateTimeOffset utcNow)
    {
        return new FrozenTimeProvider(utcNow);
    }

    public static IServiceProvider CreateServiceProvider(
        IPermissionsVersionStore? permissionsVersionStore = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        if (permissionsVersionStore is not null)
            services.AddSingleton(permissionsVersionStore);

        return services.BuildServiceProvider();
    }

    public static IHttpContextAccessor CreateHttpContextAccessor(Guid userId, string? jti = null)
    {
        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            ..CreateOptionalClaims(jti)
        ], authenticationType: "gateway"));

        return new HttpContextAccessor
        {
            HttpContext = httpContext
        };
    }

    public static ClassicCitySetupDraftDto CreateClassicCitySetupDraft(
        string name = "Novy Mir",
        string generationSeed = "seed-001",
        DateTimeOffset? startSimTimeUtc = null,
        string currentWeatherMode = "Random")
    {
        DateTimeOffset effectiveStart = startSimTimeUtc ?? new DateTimeOffset(2048, 6, 1, 8, 0, 0, TimeSpan.Zero);

        return new ClassicCitySetupDraftDto(
            Name: name,
            StartSimTimeLocal: "2048-06-01T17:00",
            StartSimTimeUtc: effectiveStart,
            SpeedMultiplier: "1.5",
            ClimateZone: "Temperate",
            Hemisphere: "Northern",
            UtcOffsetMinutes: "540",
            GenerationSeed: generationSeed,
            InitialWeatherMode: currentWeatherMode,
            InitialWeatherType: "Clear",
            InitialWeatherSeverity: "Mild",
            InitialWeatherTemperatureC: "",
            PopulationTargetMode: "Preset10K",
            SizeTier: "Medium",
            UrbanDensity: "Balanced",
            DevelopmentLevel: "Balanced",
            EconomyProfile: "Balanced",
            PopulationOccupancyProfile: "Balanced",
            PlannedPeopleCount: "");
    }

    public static ClassicCitySetupSessionState CreateClassicCitySetupSessionState(
        Guid sessionId,
        Guid ownerUserId,
        string status = "Draft",
        string currentStepId = "scenario",
        ClassicCitySetupDraftDto? draft = null,
        DateTimeOffset? updatedAtUtc = null)
    {
        DateTimeOffset timestamp = updatedAtUtc ?? new DateTimeOffset(2048, 6, 1, 9, 0, 0, TimeSpan.Zero);

        return new ClassicCitySetupSessionState
        {
            SessionId = sessionId,
            OwnerUserId = ownerUserId,
            ScenarioKind = "ClassicCity",
            Status = status,
            CurrentStepId = currentStepId,
            Draft = draft ?? CreateClassicCitySetupDraft(),
            CreatedAtUtc = timestamp.AddMinutes(-5),
            UpdatedAtUtc = timestamp
        };
    }

    public static ClassicCitySetupSessionService CreateClassicCitySetupSessionService(
        FakeClassicCitySetupSessionStore sessionStore,
        RecordingPublishEndpoint publishEndpoint,
        IHttpContextAccessor httpContextAccessor,
        FakePermissionsVersionStore? permissionsVersionStore = null,
        FakeAuthContextStore? authContextStore = null)
    {
        return new ClassicCitySetupSessionService(
            sessionStore: sessionStore,
            citiesApiClient: null!,
            provisioningService: new StubCityProvisioningService(),
            publishEndpoint: publishEndpoint,
            httpContextAccessor: httpContextAccessor,
            permissionsVersionStore: permissionsVersionStore ?? new FakePermissionsVersionStore(),
            authContextStore: authContextStore ?? new FakeAuthContextStore(),
            internalJwtRequestContextAccessor: new InternalJwtRequestContextAccessor(),
            options: CreateClassicCitySetupSessionOptions(),
            logger: NullLogger<ClassicCitySetupSessionService>.Instance);
    }

    private static IEnumerable<Claim> CreateOptionalClaims(string? jti)
    {
        if (!string.IsNullOrWhiteSpace(jti))
            yield return new Claim(JwtRegisteredClaimNames.Jti, jti);
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

    public sealed class FakePermissionsVersionStore : IPermissionsVersionStore
    {
        public int CurrentVersion { get; set; }
        public Exception? Exception { get; set; }
        public int GetCurrentCallCount { get; private set; }
        public Guid? LastRequestedUserId { get; private set; }

        public Task<int> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
        {
            GetCurrentCallCount++;
            LastRequestedUserId = userId;

            if (Exception is not null)
                throw Exception;

            return Task.FromResult(CurrentVersion);
        }
    }

    public sealed class FakeAuthContextStore : IAuthContextStore
    {
        public Dictionary<(Guid UserId, int PermissionsVersion), UserAuthContextResponse> Responses { get; } = new();
        public Exception? Exception { get; set; }
        public int GetCallCount { get; private set; }
        public (Guid UserId, int PermissionsVersion)? LastRequest { get; private set; }

        public Task<UserAuthContextResponse> GetAsync(Guid userId, int permissionsVersion, CancellationToken ct)
        {
            GetCallCount++;
            LastRequest = (userId, permissionsVersion);

            if (Exception is not null)
                throw Exception;

            if (!Responses.TryGetValue((userId, permissionsVersion), out UserAuthContextResponse? response))
                throw new KeyNotFoundException($"Auth context for user '{userId}' and version '{permissionsVersion}' was not configured.");

            return Task.FromResult(response);
        }
    }

    public sealed class FakeClassicCitySetupSessionStore : IClassicCitySetupSessionStore
    {
        public Dictionary<Guid, ClassicCitySetupSessionState> Sessions { get; } = new();
        public HashSet<Guid> TrackedSessionIds { get; } = [];
        public ClassicCitySetupSessionLockHandle? LockToReturn { get; set; } = new("lock-token");
        public ClassicCitySetupSessionLockHandle? CreateLockToReturn { get; set; } = new("create-lock-token");
        public Exception? TryAcquireLockException { get; set; }
        public Exception? TryAcquireCreateLockException { get; set; }
        public int SaveCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }
        public int ReleaseLockCallCount { get; private set; }
        public int ReleaseCreateLockCallCount { get; private set; }
        public Guid? LastDeletedSessionId { get; private set; }

        public Task<IReadOnlyList<ClassicCitySetupSessionState>> ListOwnedAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ClassicCitySetupSessionState> sessions = Sessions.Values
                .Where(x => x.OwnerUserId == ownerUserId)
                .OrderBy(x => x.SessionId)
                .ToArray();

            return Task.FromResult(sessions);
        }

        public Task DeleteAsync(Guid sessionId, Guid? ownerUserId, CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;
            LastDeletedSessionId = sessionId;
            Sessions.Remove(sessionId);
            TrackedSessionIds.Remove(sessionId);
            return Task.CompletedTask;
        }

        public Task<ClassicCitySetupSessionState?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            Sessions.TryGetValue(sessionId, out ClassicCitySetupSessionState? session);
            return Task.FromResult(session);
        }

        public Task SaveAsync(ClassicCitySetupSessionState session, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            Sessions[session.SessionId] = session;
            return Task.CompletedTask;
        }

        public Task<ClassicCitySetupSessionLockHandle?> TryAcquireLockAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            if (TryAcquireLockException is not null)
                throw TryAcquireLockException;

            return Task.FromResult(LockToReturn);
        }

        public Task<ClassicCitySetupSessionLockHandle?> TryAcquireCreateLockAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        {
            if (TryAcquireCreateLockException is not null)
                throw TryAcquireCreateLockException;

            return Task.FromResult(CreateLockToReturn);
        }

        public Task ReleaseLockAsync(Guid sessionId, ClassicCitySetupSessionLockHandle lockHandle, CancellationToken cancellationToken = default)
        {
            ReleaseLockCallCount++;
            return Task.CompletedTask;
        }

        public Task ReleaseCreateLockAsync(Guid ownerUserId, ClassicCitySetupSessionLockHandle lockHandle, CancellationToken cancellationToken = default)
        {
            ReleaseCreateLockCallCount++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Guid>> ListTrackedSessionIdsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<Guid>)TrackedSessionIds.ToArray());
        }

        public Task UntrackAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            TrackedSessionIds.Remove(sessionId);
            return Task.CompletedTask;
        }
    }

    public sealed class RecordingPublishEndpoint : IPublishEndpoint
    {
        public List<object> PublishedMessages { get; } = [];
        public Exception? Exception { get; set; }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
        {
            return new NoOpConnectHandle();
        }

        public Task Publish<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            if (Exception is not null)
                throw Exception;

            PublishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
            where T : class
        {
            return Publish(message, cancellationToken);
        }

        public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
            where T : class
        {
            return Publish(message, cancellationToken);
        }

        public Task Publish(object message, CancellationToken cancellationToken = default)
        {
            return Publish<object>(message, cancellationToken);
        }

        public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
        {
            return Publish(message, cancellationToken);
        }

        public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        {
            return Publish(message, cancellationToken);
        }

        public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        {
            return Publish(message, cancellationToken);
        }

        public Task Publish<T>(object values, CancellationToken cancellationToken = default)
            where T : class
        {
            return Publish(values, cancellationToken);
        }

        public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
            where T : class
        {
            return Publish(values, cancellationToken);
        }

        public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
            where T : class
        {
            return Publish(values, cancellationToken);
        }

        private sealed class NoOpConnectHandle : ConnectHandle
        {
            public void Dispose()
            {
            }

            public void Disconnect()
            {
            }
        }
    }

    private sealed class StubCityProvisioningService : ICityProvisioningService
    {
        public Task<Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views.CityProvisioningView> CreateCityAsync(
            Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities.CreateCityRequestDto request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Provisioning service is not used in these tests.");
        }

        public Task<Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views.CityProvisioningView> RetryPopulationBootstrapAsync(
            Guid cityId,
            int? plannedPeopleCountOverride = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Provisioning service is not used in these tests.");
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
