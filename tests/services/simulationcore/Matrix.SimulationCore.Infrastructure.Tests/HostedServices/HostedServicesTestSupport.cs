using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationCore.Infrastructure.Tests.HostedServices;

internal static class HostedServicesTestSupport
{
    internal static City CreateProvisioningCity(DateTimeOffset createdAtUtc)
    {
        return City.Create(
            name: new CityName("Recovery City"),
            simulationKind: SimulationKind.ClassicCity,
            environment: CityEnvironment.Create(
                climateZone: ClimateZone.Temperate,
                hemisphere: Hemisphere.Northern,
                utcOffset: CityUtcOffset.FromMinutes(180)),
            generationSeed: new CityGenerationSeed("recovery-seed"),
            scenarioModelSetVersion: new ScenarioModelSetVersion("classic-city-v3"),
            generationProfile: CityGenerationProfile.Create(
                sizeTier: CitySizeTier.Medium,
                urbanDensity: UrbanDensity.Balanced,
                developmentLevel: CityDevelopmentLevel.Balanced,
                economyProfile: CityEconomyProfile.Balanced,
                populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
                plannedPeopleCount: 25_000),
            initialWeatherProfile: CityInitialWeatherProfile.CreateManual(
                manualType: WeatherType.Clear,
                manualSeverity: WeatherSeverity.Calm,
                manualTemperature: TemperatureC.From(18m)),
            provisioningCorrelationId: Guid.NewGuid(),
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: false,
            createdAtUtc: createdAtUtc);
    }

    internal sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void SetUtcNow(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    internal sealed class FakeCityRepository : ICityRepository
    {
        public IReadOnlyList<City> RecoverableCities { get; set; } = Array.Empty<City>();
        public Dictionary<Guid, City?> CitiesById { get; } = [];
        public DateTimeOffset? RequestedRecoverableAsOfUtc { get; private set; }
        public int? RequestedRecoverableLimit { get; private set; }
        public int ListRecoverableCallCount { get; private set; }
        public List<CityId> RequestedCityIds { get; } = [];
        public TaskCompletionSource<bool> ListRecoverableCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> GetByIdCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<City?> GetByIdAsync(CityId cityId, CancellationToken cancellationToken)
        {
            RequestedCityIds.Add(cityId);
            GetByIdCalled.TrySetResult(true);
            CitiesById.TryGetValue(cityId.Value, out City? city);
            return Task.FromResult(city);
        }

        public Task<IReadOnlyList<City>> ListRecoverableProvisioningAsync(
            DateTimeOffset asOfUtc,
            int limit,
            CancellationToken cancellationToken)
        {
            RequestedRecoverableAsOfUtc = asOfUtc;
            RequestedRecoverableLimit = limit;
            ListRecoverableCallCount++;
            ListRecoverableCalled.TrySetResult(true);
            return Task.FromResult(RecoverableCities);
        }

        public Task<City?> GetByProvisioningCorrelationIdAsync(Guid provisioningCorrelationId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<City>> ListAsync(bool includeArchived, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<City>> ListProvisioningAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(City city, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(City city, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => throw new NotSupportedException();

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => throw new NotSupportedException();
    }

    internal sealed class FakeClassicCityProvisioningOrchestrator : IClassicCityProvisioningOrchestrator
    {
        public Guid? RequestedCityId { get; private set; }
        public string? RequestedSimulationKind { get; private set; }
        public Guid? RequestedPopulationBootstrapOperationId { get; private set; }
        public Guid? RequestedEconomyBootstrapOperationId { get; private set; }
        public int? RequestedPlannedPeopleCountOverride { get; private set; }
        public int ProvisionCallCount { get; private set; }
        public Func<Func<CancellationToken, Task>?, CancellationToken, Task>? OnProvisionAsync { get; set; }
        public TaskCompletionSource<bool> ProvisionCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CityProvisioningModel> CreateAsync(CreateCityCommand request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CityProvisioningModel> GetProvisioningViewAsync(Guid cityId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public async Task<CityProvisioningModel> ProvisionAsync(
            Guid cityId,
            string simulationKind,
            Guid populationBootstrapOperationId,
            Guid economyBootstrapOperationId,
            int? plannedPeopleCountOverride,
            Func<CancellationToken, Task>? heartbeatAsync,
            CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            RequestedSimulationKind = simulationKind;
            RequestedPopulationBootstrapOperationId = populationBootstrapOperationId;
            RequestedEconomyBootstrapOperationId = economyBootstrapOperationId;
            RequestedPlannedPeopleCountOverride = plannedPeopleCountOverride;
            ProvisionCallCount++;

            if (OnProvisionAsync is not null)
                await OnProvisionAsync(heartbeatAsync, cancellationToken);

            ProvisionCalled.TrySetResult(true);

            return new CityProvisioningModel(
                CityId: cityId,
                SimulationKind: simulationKind,
                PopulationBootstrap: new CityPopulationBootstrapModel(
                    OperationId: populationBootstrapOperationId,
                    Status: "Running",
                    PlannedPeopleCount: plannedPeopleCountOverride,
                    ResidentialCapacity: null,
                    Summary: null,
                    FailureCode: null),
                EconomyBootstrap: new CityEconomyBootstrapModel(
                    OperationId: economyBootstrapOperationId,
                    Status: "Completed",
                    FailureCode: null,
                    UnitKind: null,
                    UnitCode: null,
                    UnitDisplayName: null,
                    UnitSymbol: null));
        }
    }

    internal sealed class DictionaryServiceProvider(Dictionary<Type, object> services) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            services.TryGetValue(serviceType, out object? service);
            return service;
        }
    }

    internal sealed class TestServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new TestServiceScope(serviceProvider);
    }

    private sealed class TestServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider => serviceProvider;
        public void Dispose() { }
    }
}
