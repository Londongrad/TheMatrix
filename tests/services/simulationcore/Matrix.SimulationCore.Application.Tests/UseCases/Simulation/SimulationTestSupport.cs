using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation;

internal static class SimulationTestSupport
{
    internal static readonly DateTimeOffset SimStartTimeUtc = new(2048, 1, 2, 3, 4, 5, TimeSpan.Zero);

    internal static SimulationClock CreateClock(
        Guid? simulationId = null,
        ClockState state = ClockState.Running,
        decimal speed = 60m)
    {
        var cityId = new CityId(simulationId ?? Guid.NewGuid());
        return SimulationClock.Create(
            cityId: cityId,
            startTime: SimTime.FromUtc(SimStartTimeUtc),
            speed: SimSpeed.From(speed),
            initialState: state);
    }

    internal static SimulationHost CreateHost(
        Guid? simulationId = null,
        SimulationHostState state = SimulationHostState.Active)
    {
        var id = simulationId ?? Guid.NewGuid();
        return new SimulationHost(
            SimulationId: new SimulationId(id),
            HostId: new SimulationHostId(id),
            HostKind: SimulationHostKind.City,
            SimulationKind: SimulationKind.ClassicCity,
            State: state,
            CreatedAtUtc: SimStartTimeUtc.AddHours(-1),
            ArchivedAtUtc: state == SimulationHostState.Archived ? SimStartTimeUtc : null);
    }

    internal sealed class FakeSimulationClockRepository : ISimulationClockRepository
    {
        public SimulationClock? ClockBySimulationId { get; set; }
        public SimulationId? RequestedSimulationId { get; private set; }
        public SimulationId? DeletedSimulationId { get; private set; }
        public SimulationClock? AddedClock { get; private set; }

        public Task<SimulationClock?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            RequestedSimulationId = simulationId;
            return Task.FromResult(ClockBySimulationId);
        }

        public Task AddAsync(SimulationClock clock, CancellationToken cancellationToken)
        {
            AddedClock = clock;
            return Task.CompletedTask;
        }

        public Task DeleteBySimulationIdAsync(SimulationId simulationId, CancellationToken cancellationToken)
        {
            DeletedSimulationId = simulationId;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SimulationId>> ListActiveRunningSimulationIdsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeSimulationHostReadRepository : ISimulationHostReadRepository
    {
        public SimulationHost? HostBySimulationId { get; set; }
        public SimulationId? RequestedSimulationId { get; private set; }

        public Task<SimulationHost?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            RequestedSimulationId = simulationId;
            return Task.FromResult(HostBySimulationId);
        }
    }

    internal sealed class FakeSimulationClockMutationExecutor : ISimulationClockMutationExecutor
    {
        public SimulationId? RequestedSimulationId { get; private set; }
        public bool RequestedAllowArchivedHost { get; private set; }
        public SimulationClock? Clock { get; set; }
        public bool Result { get; set; } = true;

        public Task<bool> ExecuteAsync(
            SimulationId simulationId,
            Action<SimulationClock> mutate,
            CancellationToken cancellationToken,
            bool allowArchivedHost = false)
        {
            RequestedSimulationId = simulationId;
            RequestedAllowArchivedHost = allowArchivedHost;

            if (Clock is not null)
            {
                mutate(Clock);
            }

            return Task.FromResult(Result);
        }
    }

    internal sealed class FakeSimulationAdvanceExecutor : ISimulationAdvanceExecutor
    {
        public SimulationId? RequestedSimulationId { get; private set; }
        public TimeSpan? RequestedRealDelta { get; private set; }
        public SimulationAdvanceExecutionResult Result { get; set; } =
            new(new SimulationId(Guid.NewGuid()), SimulationAdvanceExecutionStatus.Advanced);

        public Task<SimulationAdvanceExecutionResult> ExecuteAsync(
            SimulationId simulationId,
            TimeSpan realDelta,
            CancellationToken cancellationToken)
        {
            RequestedSimulationId = simulationId;
            RequestedRealDelta = realDelta;
            return Task.FromResult(Result);
        }
    }

    internal sealed class FakeSimulationBatchAdvanceExecutor : ISimulationBatchAdvanceExecutor
    {
        public TimeSpan? RequestedRealDelta { get; private set; }
        public SimulationBatchAdvanceResult Result { get; set; } = new(0, 0, 0, 0);

        public Task<SimulationBatchAdvanceResult> ExecuteAsync(
            TimeSpan realDelta,
            CancellationToken cancellationToken)
        {
            RequestedRealDelta = realDelta;
            return Task.FromResult(Result);
        }
    }
}
