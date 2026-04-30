using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;

internal static class SimulationInfrastructureTestSupport
{
    internal sealed class FakeSimulationClockRepository : ISimulationClockRepository
    {
        public IReadOnlyList<SimulationId> ActiveSimulationIds { get; set; } = Array.Empty<SimulationId>();
        public int ListActiveRunningSimulationIdsCallCount { get; private set; }

        public Task<IReadOnlyList<SimulationId>> ListActiveRunningSimulationIdsAsync(CancellationToken cancellationToken)
        {
            ListActiveRunningSimulationIdsCallCount++;
            return Task.FromResult(ActiveSimulationIds);
        }

        public Task<SimulationClock?> GetBySimulationIdAsync(SimulationId simulationId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(SimulationClock clock, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteBySimulationIdAsync(SimulationId simulationId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeSimulationAdvanceExecutor : ISimulationAdvanceExecutor
    {
        public Dictionary<Guid, Queue<object>> OutcomesBySimulationId { get; } = [];
        public List<(SimulationId SimulationId, TimeSpan RealDelta)> Requests { get; } = [];

        public Task<SimulationAdvanceExecutionResult> ExecuteAsync(
            SimulationId simulationId,
            TimeSpan realDelta,
            CancellationToken cancellationToken)
        {
            Requests.Add((simulationId, realDelta));

            if (!OutcomesBySimulationId.TryGetValue(simulationId.Value, out Queue<object>? outcomes) || outcomes.Count == 0)
            {
                return Task.FromResult(
                    new SimulationAdvanceExecutionResult(
                        simulationId,
                        SimulationAdvanceExecutionStatus.Advanced));
            }

            object next = outcomes.Dequeue();
            if (next is Exception exception)
                throw exception;

            return Task.FromResult((SimulationAdvanceExecutionResult)next);
        }
    }

    internal sealed class FakeSimulationHostReadRepository : ISimulationHostReadRepository
    {
        public Dictionary<Guid, SimulationHost?> HostsBySimulationId { get; } = [];
        public List<SimulationId> RequestedSimulationIds { get; } = [];

        public Task<SimulationHost?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            RequestedSimulationIds.Add(simulationId);
            HostsBySimulationId.TryGetValue(simulationId.Value, out SimulationHost? host);
            return Task.FromResult(host);
        }
    }

    internal sealed class ConcurrencySaveChangesInterceptor(int failuresBeforeSuccess) : SaveChangesInterceptor
    {
        public int SaveChangesAttemptCount { get; private set; }
        public int RemainingFailures { get; private set; } = failuresBeforeSuccess;

        public void ArmFailures(int failuresBeforeSuccess)
        {
            RemainingFailures = failuresBeforeSuccess;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            SaveChangesAttemptCount++;

            if (RemainingFailures > 0)
            {
                RemainingFailures--;
                throw new DbUpdateConcurrencyException($"conflict-{SaveChangesAttemptCount}");
            }

            return new ValueTask<InterceptionResult<int>>(result);
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

    internal static SimulationCoreDbContext CreateDbContext(
        string databaseName,
        params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SimulationCoreDbContext>()
           .UseInMemoryDatabase(databaseName);

        if (interceptors.Length > 0)
            optionsBuilder.AddInterceptors(interceptors);

        return new SimulationCoreDbContext(optionsBuilder.Options);
    }

    internal static City CreateCity(DateTimeOffset createdAtUtc)
    {
        return City.Create(
            name: new CityName("Clock City"),
            simulationKind: SimulationKind.ClassicCity,
            environment: CityEnvironment.Create(
                climateZone: ClimateZone.Temperate,
                hemisphere: Hemisphere.Northern,
                utcOffset: CityUtcOffset.FromMinutes(180)),
            generationSeed: new CityGenerationSeed("clock-seed"),
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
            requiresPopulationBootstrap: false,
            requiresEconomyBootstrap: false,
            createdAtUtc: createdAtUtc);
    }

    internal static SimulationClock CreateClock(
        CityId cityId,
        DateTimeOffset startAtUtc)
    {
        return SimulationClock.Create(
            cityId: cityId,
            startTime: SimTime.FromUtc(startAtUtc),
            speed: SimSpeed.RealTime(),
            initialState: ClockState.Running);
    }

    internal static SimulationHost CreateHost(
        SimulationId simulationId,
        SimulationHostState state = SimulationHostState.Active)
    {
        return new SimulationHost(
            SimulationId: simulationId,
            HostId: new SimulationHostId(simulationId.Value),
            HostKind: SimulationHostKind.City,
            SimulationKind: SimulationKind.ClassicCity,
            State: state,
            CreatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero),
            ArchivedAtUtc: state == SimulationHostState.Archived
                ? new DateTimeOffset(2048, 2, 3, 5, 5, 6, TimeSpan.Zero)
                : null);
    }
}
