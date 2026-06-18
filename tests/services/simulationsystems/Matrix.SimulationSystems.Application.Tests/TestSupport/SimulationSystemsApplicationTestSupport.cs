using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;

namespace Matrix.SimulationSystems.Application.Tests.TestSupport
{
    internal static class SimulationSystemsApplicationTestSupport
    {
        internal static readonly Guid CityId = Guid.Parse("62000000-0000-0000-0000-000000000001");

        internal static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2052,
            month: 3,
            day: 4,
            hour: 8,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset LaterUtc = CreatedAtUtc.AddHours(2);

        internal static SimulationHostId CreateHostId()
        {
            return new SimulationHostId(CityId);
        }

        internal static CityEnvironmentalConditionState CreateState(string developmentLevel = "standard")
        {
            var policy = new CityEnvironmentalConditionPolicy();
            CityEnvironmentalConditionSnapshot seed = policy.CreateSeed(
                cityId: CityId,
                developmentLevel: developmentLevel,
                asOfUtc: CreatedAtUtc);

            return CityEnvironmentalConditionState.Create(
                simulationHostId: CreateHostId(),
                seed: seed);
        }

        internal static FrozenTimeProvider CreateTimeProvider(DateTimeOffset? utcNow = null)
        {
            return new FrozenTimeProvider(utcNow ?? CreatedAtUtc.AddHours(5));
        }

        internal static CityWeatherSystemInput CreateWeather(
            string type = "Storm",
            string severity = "Severe",
            string precipitationKind = "Rain")
        {
            return new CityWeatherSystemInput(
                Type: type,
                Severity: severity,
                PrecipitationKind: precipitationKind,
                TemperatureC: -8m,
                HumidityPercent: 82m,
                WindSpeedKph: 46m,
                CloudCoveragePercent: 88m,
                PressureHpa: 992m);
        }

        internal static CityRoadGraphTopologyDto CreateTopology(params CityDistrictTopologyDto[] districts)
        {
            return new CityRoadGraphTopologyDto(
                CityId: CityId,
                Districts: districts,
                RoadSegments: []);
        }
    }

    internal sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    internal sealed class FakeCityEnvironmentalConditionRepository : ICityEnvironmentalConditionRepository
    {
        public CityEnvironmentalConditionState? State { get; set; }
        public SimulationHostId? RequestedSimulationHostId { get; private set; }
        public int AddCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }

        public Task<CityEnvironmentalConditionState?> GetBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            RequestedSimulationHostId = simulationHostId;
            return Task.FromResult(State);
        }

        public Task<CityEnvironmentalConditionState?> GetFreshBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            RequestedSimulationHostId = simulationHostId;
            return Task.FromResult(State);
        }

        public Task<CityEnvironmentalConditionState?> GetBySimulationHostIdNoTrackingAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            RequestedSimulationHostId = simulationHostId;
            return Task.FromResult(State);
        }

        public Task AddAsync(
            CityEnvironmentalConditionState state,
            CancellationToken cancellationToken)
        {
            AddCallCount++;
            State = state;
            return Task.CompletedTask;
        }

        public Task DeleteBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            RequestedSimulationHostId = simulationHostId;
            DeleteCallCount++;
            State = null;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeCitySystemsDeletionStateRepository : ICitySystemsDeletionStateRepository
    {
        public DateTimeOffset? DeletedAtUtc { get; set; }
        public DateTimeOffset? UpdatedAtUtc { get; private set; }
        public int RecordCallCount { get; private set; }

        public Task<DateTimeOffset?> GetDeletedAtUtcAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(DeletedAtUtc);
        }

        public Task RecordAsync(
            Guid cityId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken)
        {
            DeletedAtUtc = deletedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
            RecordCallCount++;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }
        public Exception? SaveException { get; set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;

            if (SaveException is not null)
                throw SaveException;

            return Task.CompletedTask;
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            await action(cancellationToken);
        }

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            return action(cancellationToken);
        }
    }

    internal sealed class FakeCitySystemsResourceDemandOutboxWriter : ICitySystemsResourceDemandOutboxWriter
    {
        public List<ClassicCitySystemsResourceDemandSnapshotV1> Snapshots { get; } = [];

        public Task AddClassicCitySystemsResourceDemandAsync(
            ClassicCitySystemsResourceDemandSnapshotV1 snapshot,
            CancellationToken cancellationToken = default)
        {
            Snapshots.Add(snapshot);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeCityPopulationLivingConditionsOutboxWriter : ICityPopulationLivingConditionsOutboxWriter
    {
        public List<ClassicCityLivingConditionsSnapshotV1> Snapshots { get; } = [];

        public Task AddClassicCityLivingConditionsSnapshotAsync(
            ClassicCityLivingConditionsSnapshotV1 snapshot,
            CancellationToken cancellationToken = default)
        {
            Snapshots.Add(snapshot);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeCityOperationalExpenseOutboxWriter : ICityOperationalExpenseOutboxWriter
    {
        public List<ClassicCityOperationalExpenseIncurredV1> Expenses { get; } = [];

        public Task AddClassicCityOperationalExpenseAsync(
            ClassicCityOperationalExpenseIncurredV1 expense,
            CancellationToken cancellationToken = default)
        {
            Expenses.Add(expense);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeCityBudgetAuthorizationClient : ICityBudgetAuthorizationClient
    {
        public CityBudgetAuthorizationDecision? Decision { get; set; }
        public CityBudgetAuthorizationRequest? LastRequest { get; private set; }
        public int AuthorizeCallCount { get; private set; }

        public Task<CityBudgetAuthorizationDecision> AuthorizeAsync(
            CityBudgetAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            AuthorizeCallCount++;

            return Task.FromResult(
                Decision ??
                CityBudgetAuthorizationDecision.NotRequired(
                    requestedIntensity: request.RequestedIntensity,
                    pressureIndex: 0m,
                    authorizationLevel: "High",
                    availableAmount: 1_000_000m));
        }
    }

    internal sealed class FakeCityMapTopologyClient : ICityMapTopologyClient
    {
        public CityRoadGraphTopologyDto? Topology { get; set; }
        public Guid? RequestedCityId { get; private set; }
        public int GetRoadGraphCallCount { get; private set; }

        public Task<CityRoadGraphTopologyDto?> GetRoadGraphAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            GetRoadGraphCallCount++;
            return Task.FromResult(Topology);
        }
    }

    internal sealed class FakeCityOperationalTripDispatcher : ICityOperationalTripDispatcher
    {
        public int DispatchCallCount { get; private set; }
        public Guid? CityId { get; private set; }
        public Guid? FocusDistrictId { get; private set; }
        public string? Focus { get; private set; }
        public string? Intensity { get; private set; }
        public bool Result { get; set; } = true;

        public Task<bool> TryDispatchUtilityIncidentResponseAsync(
            Guid cityId,
            Guid focusDistrictId,
            string focus,
            string intensity,
            CancellationToken cancellationToken)
        {
            DispatchCallCount++;
            CityId = cityId;
            FocusDistrictId = focusDistrictId;
            Focus = focus;
            Intensity = intensity;
            return Task.FromResult(Result);
        }
    }

    internal sealed class DbUpdateConcurrencyException : Exception
    {
        public DbUpdateConcurrencyException(string message)
            : base(message) { }
    }
}
