using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;

namespace Matrix.SimulationSystems.Application.Tests.TestSupport;

internal static class SimulationSystemsApplicationTestSupport
{
    internal static readonly Guid CityId = Guid.Parse("62000000-0000-0000-0000-000000000001");
    internal static readonly DateTimeOffset CreatedAtUtc = new(2052, 3, 4, 8, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset LaterUtc = CreatedAtUtc.AddHours(2);

    internal static SimulationHostId CreateHostId()
    {
        return new SimulationHostId(CityId);
    }

    internal static CityEnvironmentalConditionState CreateState(
        string developmentLevel = "standard")
    {
        var policy = new CityEnvironmentalConditionPolicy();
        var seed = policy.CreateSeed(
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
}

internal sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal sealed class FakeCityEnvironmentalConditionRepository : ICityEnvironmentalConditionRepository
{
    public CityEnvironmentalConditionState? State { get; set; }
    public SimulationHostId? RequestedSimulationHostId { get; private set; }
    public int AddCallCount { get; private set; }

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

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        throw new NotSupportedException();
    }

    public Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        throw new NotSupportedException();
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

internal sealed class DbUpdateConcurrencyException : Exception
{
    public DbUpdateConcurrencyException(string message)
        : base(message)
    {
    }
}
