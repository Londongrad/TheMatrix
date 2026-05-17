using System.Reflection;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Weather.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.ClassicCity;

public sealed class GatewayInfrastructureServicesTests
{
    [Fact]
    public async Task CityProvisioningServiceCreateCityAsync_MapsGatewayDtoToDownstreamRequest()
    {
        Guid correlationId = Guid.Parse("319a18f0-5fcf-419f-a646-d7ac67503e7b");
        var citiesClient = new RecordingProvisioningCitiesClient
        {
            CreateProvisionedCityResult = new CityProvisioningView(
                CityId: Guid.Parse("bfcd3260-b9a7-445b-9474-f8ff8ad5691c"),
                SimulationKind: "ClassicCity",
                PopulationBootstrap: CreatePopulationBootstrapView(),
                EconomyBootstrap: CreateEconomyBootstrapView())
        };
        var service = new CityProvisioningService(citiesClient);
        CreateCityRequestDto request = new(
            Name: "Novy Mir",
            StartSimTimeUtc: new DateTimeOffset(2048, 6, 15, 8, 0, 0, TimeSpan.Zero),
            SpeedMultiplier: 1.5m,
            SimulationKind: "ClassicCity",
            ClimateZone: "Temperate",
            Hemisphere: "Northern",
            UtcOffsetMinutes: 540,
            GenerationSeed: "seed-77",
            SizeTier: "Medium",
            UrbanDensity: "Balanced",
            DevelopmentLevel: "Growing",
            EconomyProfile: "Balanced",
            PopulationOccupancyProfile: "Balanced",
            InitialWeatherMode: "Scripted",
            InitialWeatherType: "Rain",
            InitialWeatherSeverity: "Moderate",
            InitialWeatherTemperatureC: 12.5m,
            PlannedPeopleCount: 12000,
            ProvisioningCorrelationId: correlationId);

        CityProvisioningView result = await service.CreateCityAsync(request, CancellationToken.None);

        Assert.Equal(citiesClient.CreateProvisionedCityResult.CityId, result.CityId);
        Assert.NotNull(citiesClient.LastCreateProvisionedRequest);
        CreateCityRequest downstream = citiesClient.LastCreateProvisionedRequest!;
        Assert.Equal("Novy Mir", downstream.Name);
        Assert.Equal("Growing", downstream.DevelopmentLevel);
        Assert.Equal(12.5m, downstream.InitialWeatherTemperatureC);
        Assert.Equal(12000, downstream.PlannedPeopleCount);
        Assert.Equal(correlationId, downstream.ProvisioningCorrelationId);
    }

    [Fact]
    public async Task CityProvisioningServiceRetryPopulationBootstrapAsync_MapsOverrideValue()
    {
        Guid cityId = Guid.Parse("883af998-f53b-4b39-b576-bad953ab0a69");
        var citiesClient = new RecordingProvisioningCitiesClient
        {
            RetryProvisioningResult = new CityProvisioningView(
                CityId: cityId,
                SimulationKind: "ClassicCity",
                PopulationBootstrap: CreatePopulationBootstrapView(),
                EconomyBootstrap: CreateEconomyBootstrapView())
        };
        var service = new CityProvisioningService(citiesClient);

        CityProvisioningView result = await service.RetryPopulationBootstrapAsync(cityId, 14000, CancellationToken.None);

        Assert.Equal(cityId, result.CityId);
        Assert.Equal(cityId, citiesClient.LastRetryCityId);
        Assert.Equal(14000, citiesClient.LastRetryRequest?.PlannedPeopleCountOverride);
    }

    [Fact]
    public async Task ClassicCitySetupSessionRecoveryHostedService_ReconcileOnceAsync_ReconcilesAllTrackedSessions()
    {
        var store = new RecordingSetupSessionStore
        {
            TrackedSessionIds =
            [
                Guid.Parse("9d52fdc4-d529-49ba-a74d-191061ace3b2"),
                Guid.Parse("8f5efc09-93ea-4d14-ae7e-d1f17c6a9b4a")
            ]
        };
        var service = new RecordingReconciliationService();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IClassicCitySetupSessionStore>(store)
            .AddSingleton<IClassicCitySetupSessionService>(service)
            .BuildServiceProvider();
        var hostedService = new ClassicCitySetupSessionRecoveryHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ClassicCitySetupSessionOptions()),
            NullLogger<ClassicCitySetupSessionRecoveryHostedService>.Instance);

        MethodInfo method = typeof(ClassicCitySetupSessionRecoveryHostedService)
            .GetMethod("ReconcileOnceAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ReconcileOnceAsync method was not found.");
        Task task = Assert.IsAssignableFrom<Task>(method.Invoke(hostedService, [CancellationToken.None]));
        await task;

        Assert.Equal(store.TrackedSessionIds, service.ReconciledSessionIds);
    }

    [Fact]
    public async Task ClassicCitySetupSessionRecoveryHostedService_ReconcileOnceAsync_ContinuesAfterSessionFailure()
    {
        Guid first = Guid.Parse("fe8adf64-b1e5-4bdd-96a5-7e9727c573f3");
        Guid second = Guid.Parse("f8a6449e-042e-47d7-826a-a515de4efd72");
        var store = new RecordingSetupSessionStore
        {
            TrackedSessionIds = [first, second]
        };
        var service = new RecordingReconciliationService
        {
            FailOnSessionId = first
        };
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IClassicCitySetupSessionStore>(store)
            .AddSingleton<IClassicCitySetupSessionService>(service)
            .BuildServiceProvider();
        var hostedService = new ClassicCitySetupSessionRecoveryHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ClassicCitySetupSessionOptions()),
            NullLogger<ClassicCitySetupSessionRecoveryHostedService>.Instance);

        MethodInfo method = typeof(ClassicCitySetupSessionRecoveryHostedService)
            .GetMethod("ReconcileOnceAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ReconcileOnceAsync method was not found.");
        Task task = Assert.IsAssignableFrom<Task>(method.Invoke(hostedService, [CancellationToken.None]));
        await task;

        Assert.Equal([first, second], service.ReconciledSessionIds);
    }

    private sealed class RecordingProvisioningCitiesClient : ICitiesApiClient
    {
        public CreateCityRequest? LastCreateProvisionedRequest { get; private set; }
        public Guid? LastRetryCityId { get; private set; }
        public RetryCityPopulationBootstrapProvisioningRequest? LastRetryRequest { get; private set; }
        public CityProvisioningView CreateProvisionedCityResult { get; set; } = new(
            CityId: Guid.NewGuid(),
            SimulationKind: "ClassicCity",
            PopulationBootstrap: CreatePopulationBootstrapView(),
            EconomyBootstrap: CreateEconomyBootstrapView());
        public CityProvisioningView RetryProvisioningResult { get; set; } = new(
            CityId: Guid.NewGuid(),
            SimulationKind: "ClassicCity",
            PopulationBootstrap: CreatePopulationBootstrapView(),
            EconomyBootstrap: CreateEconomyBootstrapView());

        public Task<CityCreatedView> CreateCityAsync(CreateCityRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityProvisioningView> CreateProvisionedCityAsync(CreateCityRequest request, CancellationToken cancellationToken = default)
        {
            LastCreateProvisionedRequest = request;
            return Task.FromResult(CreateProvisionedCityResult);
        }

        public Task<IReadOnlyList<SimulationKindCatalogItemView>> GetSimulationKindsAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<CityListItemView>> ListCitiesAsync(bool includeArchived, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<CityListItemView>> ListProvisioningCitiesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityView> GetCityAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityProvisioningStatusView> GetProvisioningStatusAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityWeatherView> GetWeatherAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityMapTopologyView> GetMapAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ResidentialBuildingView>> GetResidentialBuildingsAsync(Guid cityId, Guid? districtId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityPopulationBootstrapRestartedView> RestartPopulationBootstrapAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityProvisioningView> RetryPopulationBootstrapProvisioningAsync(Guid cityId, RetryCityPopulationBootstrapProvisioningRequest request, CancellationToken cancellationToken = default)
        {
            LastRetryCityId = cityId;
            LastRetryRequest = request;
            return Task.FromResult(RetryProvisioningResult);
        }

        public Task CompletePopulationBootstrapAsync(Guid cityId, CompleteCityPopulationBootstrapRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task CompleteEconomyBootstrapAsync(Guid cityId, CompleteCityEconomyBootstrapRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task FailPopulationBootstrapAsync(Guid cityId, FailCityPopulationBootstrapRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task FailEconomyBootstrapAsync(Guid cityId, FailCityEconomyBootstrapRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task UpdateEnvironmentAsync(Guid cityId, UpdateCityEnvironmentRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RenameCityAsync(Guid cityId, RenameCityRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ArchiveCityAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteCityAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingSetupSessionStore : IClassicCitySetupSessionStore
    {
        public IReadOnlyList<Guid> TrackedSessionIds { get; set; } = [];

        public Task<IReadOnlyList<ClassicCitySetupSessionState>> ListOwnedAsync(Guid ownerUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Guid sessionId, Guid? ownerUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ClassicCitySetupSessionState?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(ClassicCitySetupSessionState session, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ClassicCitySetupSessionLockHandle?> TryAcquireLockAsync(Guid sessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ClassicCitySetupSessionLockHandle?> TryAcquireCreateLockAsync(Guid ownerUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReleaseLockAsync(Guid sessionId, ClassicCitySetupSessionLockHandle lockHandle, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReleaseCreateLockAsync(Guid ownerUserId, ClassicCitySetupSessionLockHandle lockHandle, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UntrackAsync(Guid sessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> ListTrackedSessionIdsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(TrackedSessionIds);
    }

    private sealed class RecordingReconciliationService : IClassicCitySetupSessionService
    {
        public Guid? FailOnSessionId { get; set; }
        public List<Guid> ReconciledSessionIds { get; } = [];

        public Task<IReadOnlyList<ClassicCitySetupSessionView>> ListDraftsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ClassicCitySetupSessionView> CreateAsync(CreateClassicCitySetupSessionRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ClassicCitySetupSessionView?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ClassicCitySetupSessionMutationResult> DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ClassicCitySetupSessionMutationResult> UpdateAsync(Guid sessionId, UpdateClassicCitySetupSessionRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ClassicCitySetupSessionMutationResult> QueueLaunchAsync(Guid sessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ProcessLaunchAsync(Guid sessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task ReconcileAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            ReconciledSessionIds.Add(sessionId);

            if (FailOnSessionId == sessionId)
                throw new InvalidOperationException("Reconcile failed.");

            return Task.CompletedTask;
        }
    }

    private static CityPopulationBootstrapView CreatePopulationBootstrapView()
    {
        return new CityPopulationBootstrapView(
            OperationId: Guid.NewGuid(),
            Status: "Pending",
            PlannedPeopleCount: null,
            ResidentialCapacity: null,
            Summary: null,
            FailureCode: null);
    }

    private static CityEconomyBootstrapView CreateEconomyBootstrapView()
    {
        return new CityEconomyBootstrapView(
            OperationId: Guid.NewGuid(),
            Status: "Pending",
            FailureCode: null,
            UnitKind: null,
            UnitCode: null,
            UnitDisplayName: null,
            UnitSymbol: null);
    }
}
