using System.Data;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;

namespace Matrix.Resources.Application.Tests.TestSupport;

internal static class ResourcesApplicationTestSupport
{
    internal static readonly DateTimeOffset CreatedAtUtc = new(2049, 1, 1, 8, 0, 0, TimeSpan.Zero);
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
            Fuel: CreateLine(CityResourceKind.Fuel, 0.62m, 0.51m, 0.53m, 0.41m),
            Food: CreateLine(CityResourceKind.Food, 0.77m, 0.33m, 0.66m, 0.24m),
            Medicine: CreateLine(CityResourceKind.Medicine, 0.58m, 0.45m, 0.50m, 0.43m),
            SpareParts: CreateLine(CityResourceKind.SpareParts, 0.61m, 0.47m, 0.49m, 0.42m),
            Filters: CreateLine(CityResourceKind.Filters, 0.65m, 0.38m, 0.54m, 0.36m),
            EmergencyWater: CreateLine(CityResourceKind.EmergencyWater, 0.74m, 0.31m, 0.61m, 0.28m),
            SystemsDemand: new CitySystemsResourceDemandSnapshot(0.20m, 0.22m, 0.18m, 0.16m, 0.19m, 3, evaluatedAtUtc ?? CreatedAtUtc),
            OperationalBudgetPressure: new CityOperationalBudgetPressureSnapshot(300_000m, 35_000m, 120_000m, 110_000m, 100_000m, 90_000m, "High", "Medium", "Medium", "Low", 0.31m, 3, evaluatedAtUtc ?? CreatedAtUtc),
            SupplyStressIndex: emergencyRationingEnabled ? 0.25m : 0.33m,
            EmergencyRationingEnabled: emergencyRationingEnabled,
            EvaluatedAtUtc: evaluatedAtUtc ?? CreatedAtUtc);

        return CityStockpileState.Create(CreateHostId(), snapshot);
    }

    internal static CityStockpileLineSnapshot CreateLine(
        CityResourceKind kind,
        decimal stockLevelIndex,
        decimal demandPressureIndex,
        decimal resupplyReadinessIndex,
        decimal shortageRiskIndex)
    {
        return new CityStockpileLineSnapshot(kind, stockLevelIndex, demandPressureIndex, resupplyReadinessIndex, shortageRiskIndex);
    }

    internal static FrozenTimeProvider CreateTimeProvider(DateTimeOffset? utcNow = null)
    {
        return new FrozenTimeProvider(utcNow ?? LaterUtc);
    }
}

internal sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal sealed class FakeCityStockpileRepository : ICityStockpileRepository
{
    public CityStockpileState? State { get; set; }
    public SimulationHostId? RequestedSimulationHostId { get; private set; }
    public int AddCallCount { get; private set; }

    public Task<CityStockpileState?> GetBySimulationHostIdAsync(SimulationHostId simulationHostId, CancellationToken cancellationToken)
    {
        RequestedSimulationHostId = simulationHostId;
        return Task.FromResult(State);
    }

    public Task AddAsync(CityStockpileState state, CancellationToken cancellationToken)
    {
        AddCallCount++;
        State = state;
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

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        throw new NotSupportedException();
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        throw new NotSupportedException();
    }
}

internal sealed class FakeCityStockpileSnapshotOutboxWriter : ICityStockpileSnapshotOutboxWriter
{
    public List<ClassicCityStockpileSnapshotV1> Snapshots { get; } = [];

    public Task AddClassicCityStockpileSnapshotAsync(ClassicCityStockpileSnapshotV1 snapshot, CancellationToken cancellationToken = default)
    {
        Snapshots.Add(snapshot);
        return Task.CompletedTask;
    }
}

internal sealed class FakeCityOperationalExpenseOutboxWriter : ICityOperationalExpenseOutboxWriter
{
    public List<ClassicCityOperationalExpenseIncurredV1> Expenses { get; } = [];

    public Task AddClassicCityOperationalExpenseAsync(ClassicCityOperationalExpenseIncurredV1 expense, CancellationToken cancellationToken = default)
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

    public Task<CityBudgetAuthorizationDecision> AuthorizeAsync(CityBudgetAuthorizationRequest request, CancellationToken cancellationToken)
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
