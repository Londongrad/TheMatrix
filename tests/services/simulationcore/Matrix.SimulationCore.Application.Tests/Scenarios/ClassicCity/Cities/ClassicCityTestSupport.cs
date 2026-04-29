using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Application.Services.Bootstrap.Abstractions;
using Matrix.SimulationCore.Application.Services.Generation.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
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
        bool requiresEconomyBootstrap = false,
        Guid? provisioningCorrelationId = null)
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
            provisioningCorrelationId: provisioningCorrelationId,
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
        public int GetByProvisioningCorrelationCallCount { get; private set; }
        public City? CityByProvisioningCorrelationId { get; set; }
        public Queue<City?> CityByProvisioningCorrelationSequence { get; } = [];
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
            GetByProvisioningCorrelationCallCount++;
            RequestedProvisioningCorrelationId = provisioningCorrelationId;
            City? city = CityByProvisioningCorrelationSequence.Count > 0
                ? CityByProvisioningCorrelationSequence.Dequeue()
                : CityByProvisioningCorrelationId;
            return Task.FromResult(city);
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
        public CreateCityCommand? RequestedCommand { get; private set; }
        public CitySimulationBootstrapPlan? Plan { get; init; }

        public CitySimulationBootstrapPlan CreatePlan(CreateCityCommand request)
        {
            RequestedCommand = request;
            return Plan ?? throw new NotSupportedException();
        }
    }

    internal sealed class FakeSimulationCoreOutboxWriter : ISimulationCoreOutboxWriter
    {
        public IReadOnlyList<IDomainEvent> CityEvents { get; private set; } = Array.Empty<IDomainEvent>();
        public IReadOnlyList<IDomainEvent> WeatherEvents { get; private set; } = Array.Empty<IDomainEvent>();
        public List<CityTimeAdvancedCall> CityTimeAdvancedCalls { get; } = [];
        public List<CityTickPhaseReachedCall> CityTickPhaseReachedCalls { get; } = [];

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
            CancellationToken cancellationToken)
        {
            CityTimeAdvancedCalls.Add(new CityTimeAdvancedCall(
                cityId,
                simulationId,
                simulationKind,
                from,
                to,
                tickId,
                speed,
                phase));
            return Task.CompletedTask;
        }

        public Task AddCityTickPhaseReachedAsync(
            CityId cityId,
            SimulationId simulationId,
            SimulationKind simulationKind,
            SimTime from,
            SimTime to,
            TickId tickId,
            SimSpeed speed,
            CityTickPhase phase,
            CancellationToken cancellationToken)
        {
            CityTickPhaseReachedCalls.Add(new CityTickPhaseReachedCall(
                cityId,
                simulationId,
                simulationKind,
                from,
                to,
                tickId,
                speed,
                phase));
            return Task.CompletedTask;
        }

        public sealed record CityTimeAdvancedCall(
            CityId CityId,
            SimulationId SimulationId,
            SimulationKind SimulationKind,
            SimTime From,
            SimTime To,
            TickId TickId,
            SimSpeed Speed,
            CityTickPhase Phase);

        public sealed record CityTickPhaseReachedCall(
            CityId CityId,
            SimulationId SimulationId,
            SimulationKind SimulationKind,
            SimTime From,
            SimTime To,
            TickId TickId,
            SimSpeed Speed,
            CityTickPhase Phase);
    }
}
