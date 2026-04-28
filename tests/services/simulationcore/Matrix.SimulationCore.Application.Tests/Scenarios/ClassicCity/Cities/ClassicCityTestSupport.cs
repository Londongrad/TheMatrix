using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Application.Services.Bootstrap.Abstractions;
using Matrix.SimulationCore.Application.Services.Generation.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;

internal static class ClassicCityTestSupport
{
    internal static readonly DateTimeOffset CreatedAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);

    internal static City CreateCity(
        string name = "Alpha City",
        bool requiresPopulationBootstrap = false,
        bool requiresEconomyBootstrap = false)
    {
        return City.Create(
            name: new CityName(name),
            simulationKind: SimulationKind.ClassicCity,
            environment: CityEnvironment.Create(
                climateZone: ClimateZone.Temperate,
                hemisphere: Hemisphere.Northern,
                utcOffset: CityUtcOffset.FromMinutes(180)),
            generationSeed: new CityGenerationSeed("alpha-seed"),
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
            provisioningCorrelationId: null,
            requiresPopulationBootstrap: requiresPopulationBootstrap,
            requiresEconomyBootstrap: requiresEconomyBootstrap,
            createdAtUtc: CreatedAtUtc);
    }

    internal sealed class FakeCityRepository : ICityRepository
    {
        public City? CityById { get; set; }
        public IReadOnlyList<City> Cities { get; set; } = Array.Empty<City>();
        public IReadOnlyList<City> ProvisioningCities { get; set; } = Array.Empty<City>();
        public CityId? RequestedCityId { get; private set; }
        public bool? RequestedIncludeArchived { get; private set; }
        public bool ListProvisioningRequested { get; private set; }
        public Guid? RequestedProvisioningCorrelationId { get; private set; }
        public City? CityByProvisioningCorrelationId { get; set; }
        public City? AddedCity { get; private set; }
        public City? DeletedCity { get; private set; }
        public int GetByIdCallCount { get; private set; }

        public Task<City?> GetByIdAsync(CityId cityId, CancellationToken cancellationToken)
        {
            GetByIdCallCount++;
            RequestedCityId = cityId;
            return Task.FromResult(CityById);
        }

        public Task<IReadOnlyList<City>> ListAsync(bool includeArchived, CancellationToken cancellationToken)
        {
            RequestedIncludeArchived = includeArchived;
            return Task.FromResult(Cities);
        }

        public Task<IReadOnlyList<City>> ListProvisioningAsync(CancellationToken cancellationToken)
        {
            ListProvisioningRequested = true;
            return Task.FromResult(ProvisioningCities);
        }

        public Task<City?> GetByProvisioningCorrelationIdAsync(Guid provisioningCorrelationId, CancellationToken cancellationToken)
        {
            RequestedProvisioningCorrelationId = provisioningCorrelationId;
            return Task.FromResult(CityByProvisioningCorrelationId);
        }
        public Task<IReadOnlyList<City>> ListRecoverableProvisioningAsync(DateTimeOffset asOfUtc, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task AddAsync(City city, CancellationToken cancellationToken)
        {
            AddedCity = city;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(City city, CancellationToken cancellationToken)
        {
            DeletedCity = city;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeCityNameSuggestionService : ICityNameSuggestionService
    {
        public string? RequestedSeed { get; private set; }
        public int? RequestedCount { get; private set; }
        public IReadOnlyList<string> Result { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> GetSuggestions(string? seed, int count)
        {
            RequestedSeed = seed;
            RequestedCount = count;
            return Result;
        }
    }

    internal sealed class FakeCityGenerationContentCatalog : ICityGenerationContentCatalog
    {
        public IReadOnlyList<string> CityNamePresets { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> DistrictNamePresets { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> StreetNamePresets { get; init; } = Array.Empty<string>();
    }

    internal sealed class FakeCitySimulationBootstrapStrategy : ICitySimulationBootstrapStrategy
    {
        public SimulationKind Kind => Descriptor.Kind;
        public required SimulationKindDescriptor Descriptor { get; init; }

        public CitySimulationBootstrapPlan CreatePlan(Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity.CreateCityCommand request)
            => throw new NotSupportedException();
    }

    internal sealed class FakeSimulationCoreOutboxWriter : ISimulationCoreOutboxWriter
    {
        public IReadOnlyList<IDomainEvent> CityEvents { get; private set; } = Array.Empty<IDomainEvent>();
        public IReadOnlyList<IDomainEvent> WeatherEvents { get; private set; } = Array.Empty<IDomainEvent>();

        public Task AddCityEventsAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            CityEvents = domainEvents.ToArray();
            return Task.CompletedTask;
        }

        public Task AddWeatherEventsAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            WeatherEvents = domainEvents.ToArray();
            return Task.CompletedTask;
        }

        public Task AddCityTimeAdvancedAsync(
            CityId cityId,
            SimulationId simulationId,
            SimulationKind simulationKind,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            CityTickPhase phase,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task AddCityTickPhaseReachedAsync(
            CityId cityId,
            SimulationId simulationId,
            SimulationKind simulationKind,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            CityTickPhase phase,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
