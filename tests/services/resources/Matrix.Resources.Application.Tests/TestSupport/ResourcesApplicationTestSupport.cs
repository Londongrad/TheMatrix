using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;

namespace Matrix.Resources.Application.Tests.TestSupport
{
    internal static class ResourcesApplicationTestSupport
    {
        internal static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2049,
            month: 1,
            day: 1,
            hour: 8,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset LaterUtc = CreatedAtUtc.AddHours(3);

        internal static Guid CityId => Guid.Parse("50000000-0000-0000-0000-000000000001");

        internal static SimulationHostId CreateHostId()
        {
            return new SimulationHostId(CityId);
        }

        internal static CityStockpileState CreateState(
            bool emergencyRationingEnabled = false,
            DateTimeOffset? evaluatedAtUtc = null)
        {
            CityStockpileSnapshot snapshot = new(
                Fuel: CreateLine(
                    kind: CityResourceKind.Fuel,
                    stockLevelIndex: 0.62m,
                    demandPressureIndex: 0.51m,
                    resupplyReadinessIndex: 0.53m,
                    shortageRiskIndex: 0.41m),
                Food: CreateLine(
                    kind: CityResourceKind.Food,
                    stockLevelIndex: 0.77m,
                    demandPressureIndex: 0.33m,
                    resupplyReadinessIndex: 0.66m,
                    shortageRiskIndex: 0.24m),
                Medicine: CreateLine(
                    kind: CityResourceKind.Medicine,
                    stockLevelIndex: 0.58m,
                    demandPressureIndex: 0.45m,
                    resupplyReadinessIndex: 0.50m,
                    shortageRiskIndex: 0.43m),
                SpareParts: CreateLine(
                    kind: CityResourceKind.SpareParts,
                    stockLevelIndex: 0.61m,
                    demandPressureIndex: 0.47m,
                    resupplyReadinessIndex: 0.49m,
                    shortageRiskIndex: 0.42m),
                Filters: CreateLine(
                    kind: CityResourceKind.Filters,
                    stockLevelIndex: 0.65m,
                    demandPressureIndex: 0.38m,
                    resupplyReadinessIndex: 0.54m,
                    shortageRiskIndex: 0.36m),
                EmergencyWater: CreateLine(
                    kind: CityResourceKind.EmergencyWater,
                    stockLevelIndex: 0.74m,
                    demandPressureIndex: 0.31m,
                    resupplyReadinessIndex: 0.61m,
                    shortageRiskIndex: 0.28m),
                SystemsDemand: new CitySystemsResourceDemandSnapshot(
                    FuelDemandPressureIndex: 0.20m,
                    SparePartsDemandPressureIndex: 0.22m,
                    FiltersDemandPressureIndex: 0.18m,
                    EmergencyWaterDemandPressureIndex: 0.16m,
                    OverallDemandPressureIndex: 0.19m,
                    EffectiveTickId: 3,
                    EffectiveAtUtc: evaluatedAtUtc ?? CreatedAtUtc),
                OperationalBudgetPressure: new CityOperationalBudgetPressureSnapshot(
                    Balance: 300_000m,
                    MunicipalOperationsExpenses: 35_000m,
                    GeneralAvailableAmount: 120_000m,
                    OperationsAvailableAmount: 110_000m,
                    InfrastructureAvailableAmount: 100_000m,
                    HealthcareAvailableAmount: 90_000m,
                    GeneralAuthorizationLevel: "High",
                    OperationsAuthorizationLevel: "Medium",
                    InfrastructureAuthorizationLevel: "Medium",
                    HealthcareAuthorizationLevel: "Low",
                    PressureIndex: 0.31m,
                    EffectiveTickId: 3,
                    EffectiveAtUtc: evaluatedAtUtc ?? CreatedAtUtc),
                SupplyStressIndex: emergencyRationingEnabled
                    ? 0.25m
                    : 0.33m,
                EmergencyRationingEnabled: emergencyRationingEnabled,
                EvaluatedAtUtc: evaluatedAtUtc ?? CreatedAtUtc);

            return CityStockpileState.Create(
                simulationHostId: CreateHostId(),
                seed: snapshot);
        }

        internal static CityStockpileLineSnapshot CreateLine(
            CityResourceKind kind,
            decimal stockLevelIndex,
            decimal demandPressureIndex,
            decimal resupplyReadinessIndex,
            decimal shortageRiskIndex)
        {
            return new CityStockpileLineSnapshot(
                Kind: kind,
                StockLevelIndex: stockLevelIndex,
                DemandPressureIndex: demandPressureIndex,
                ResupplyReadinessIndex: resupplyReadinessIndex,
                ShortageRiskIndex: shortageRiskIndex);
        }

        internal static FrozenTimeProvider CreateTimeProvider(DateTimeOffset? utcNow = null)
        {
            return new FrozenTimeProvider(utcNow ?? LaterUtc);
        }
    }

    internal sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    internal sealed class FakeCityStockpileRepository : ICityStockpileRepository
    {
        public CityStockpileState? State { get; set; }
        public SimulationHostId? RequestedSimulationHostId { get; private set; }
        public int AddCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }

        public Task<CityStockpileState?> GetBySimulationHostIdAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            RequestedSimulationHostId = simulationHostId;
            return Task.FromResult(State);
        }

        public Task AddAsync(
            CityStockpileState state,
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

    internal sealed class FakeCityResourceDeletionStateRepository : ICityResourceDeletionStateRepository
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

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
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

    internal sealed class FakeCityStockpileSnapshotOutboxWriter : ICityStockpileSnapshotOutboxWriter
    {
        public List<ClassicCityStockpileSnapshotV1> Snapshots { get; } = [];

        public Task AddClassicCityStockpileSnapshotAsync(
            ClassicCityStockpileSnapshotV1 snapshot,
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
        public CityBudgetAuthorizationDecision Response { get; set; } = CityBudgetAuthorizationDecision.NotRequired(
            requestedIntensity: "Low",
            pressureIndex: 0m,
            authorizationLevel: "High",
            availableAmount: 100_000m);

        public CityBudgetAuthorizationRequest? Request { get; private set; }
        public int CallCount { get; private set; }

        public Task<CityBudgetAuthorizationDecision> AuthorizeAsync(
            CityBudgetAuthorizationRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            CallCount++;
            return Task.FromResult(Response);
        }
    }

    internal sealed class FakeCityResupplyTripDispatcher : ICityResupplyTripDispatcher
    {
        public bool Result { get; set; } = true;
        public int CallCount { get; private set; }
        public Guid? CityId { get; private set; }
        public Guid? FocusDistrictId { get; private set; }
        public string? Focus { get; private set; }
        public string? Intensity { get; private set; }

        public Task<bool> TryDispatchDistrictResupplyAsync(
            Guid cityId,
            Guid focusDistrictId,
            string focus,
            string intensity,
            CancellationToken cancellationToken)
        {
            CallCount++;
            CityId = cityId;
            FocusDistrictId = focusDistrictId;
            Focus = focus;
            Intensity = intensity;
            return Task.FromResult(Result);
        }
    }
}
