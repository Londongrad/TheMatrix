using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation
{
    internal static class SimulationTestSupport
    {
        internal static readonly DateTimeOffset SimStartTimeUtc = new(
            year: 2048,
            month: 1,
            day: 2,
            hour: 3,
            minute: 4,
            second: 5,
            offset: TimeSpan.Zero);

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
            Guid id = simulationId ?? Guid.NewGuid();
            return new SimulationHost(
                SimulationId: new SimulationId(id),
                HostId: new SimulationHostId(id),
                HostKind: SimulationHostKind.City,
                SimulationKind: SimulationKind.ClassicCity,
                State: state,
                CreatedAtUtc: SimStartTimeUtc.AddHours(-1),
                ArchivedAtUtc: state == SimulationHostState.Archived
                    ? SimStartTimeUtc
                    : null);
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

            public Task AddAsync(
                SimulationClock clock,
                CancellationToken cancellationToken)
            {
                AddedClock = clock;
                return Task.CompletedTask;
            }

            public Task DeleteBySimulationIdAsync(
                SimulationId simulationId,
                CancellationToken cancellationToken)
            {
                DeletedSimulationId = simulationId;
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<SimulationId>> ListActiveRunningSimulationIdsAsync(
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
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
                    mutate(Clock);

                return Task.FromResult(Result);
            }
        }

        internal sealed class FakeSimulationAdvanceExecutor : ISimulationAdvanceExecutor
        {
            public SimulationId? RequestedSimulationId { get; private set; }
            public TimeSpan? RequestedRealDelta { get; private set; }

            public SimulationAdvanceExecutionResult Result { get; set; } =
                new(
                    SimulationId: new SimulationId(Guid.NewGuid()),
                    Status: SimulationAdvanceExecutionStatus.Advanced,
                    StepsProcessed: 1);

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

            public SimulationBatchAdvanceResult Result { get; set; } = new(
                ProcessedCount: 0,
                AdvancedCount: 0,
                NoStepDueCount: 0,
                LaggingCount: 0,
                FailedCount: 0,
                TotalStepsProcessed: 0);

            public Task<SimulationBatchAdvanceResult> ExecuteAsync(
                TimeSpan realDelta,
                CancellationToken cancellationToken)
            {
                RequestedRealDelta = realDelta;
                return Task.FromResult(Result);
            }
        }

        internal sealed class FakeSimulationFixedStepSettings : ISimulationFixedStepSettings
        {
            public int FixedStepSeconds { get; init; } = 60;
            public int MaxStepsPerSimulationPerCycle { get; init; } = 10;
        }

        internal sealed class FakeUnitOfWork : IUnitOfWork
        {
            public int SaveChangesCallCount { get; private set; }
            public int ExecuteInTransactionCallCount { get; private set; }
            public IsolationLevel? RequestedIsolationLevel { get; private set; }

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
                ExecuteInTransactionCallCount++;
                RequestedIsolationLevel = isolationLevel;
                await action(cancellationToken);
            }

            public Task<T> ExecuteInTransactionAsync<T>(
                Func<CancellationToken, Task<T>> action,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                throw new NotSupportedException();
            }
        }

        internal sealed class FakeSimulationScenarioAdvanceHandler : ISimulationScenarioAdvanceHandler
        {
            public SimulationHost? RequestedHost { get; private set; }
            public SimulationTimeAdvancedDomainEvent? RequestedAdvancedEvent { get; private set; }
            public List<SimulationTimeAdvancedDomainEvent> RequestedAdvancedEvents { get; } = [];
            public int HandleCallCount { get; private set; }
            public SimulationHostKind HostKind { get; init; } = SimulationHostKind.City;

            public Task HandleAdvancedAsync(
                SimulationHost host,
                SimulationTimeAdvancedDomainEvent advancedEvent,
                CancellationToken cancellationToken)
            {
                HandleCallCount++;
                RequestedHost = host;
                RequestedAdvancedEvent = advancedEvent;
                RequestedAdvancedEvents.Add(advancedEvent);
                return Task.CompletedTask;
            }
        }
    }
}
