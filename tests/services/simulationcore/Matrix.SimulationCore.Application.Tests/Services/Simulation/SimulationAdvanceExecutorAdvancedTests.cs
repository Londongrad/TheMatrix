using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Services.Simulation
{
    public sealed class SimulationAdvanceExecutorAdvancedTests
    {
        [Fact]
        public async Task ExecuteAsync_WhenOneFixedStepIsDue_AdvancesClockAndDispatchesOneStep()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(speed: 60m);
            clock.ClearDomainEvents();
            SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
            var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
            SimulationAdvanceExecutor executor = CreateExecutor(
                clock: clock,
                host: host,
                handler);

            SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                simulationId: clock.SimulationId,
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SimulationAdvanceExecutionStatus.Advanced,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: result.StepsProcessed);
            Assert.Equal(
                expected: 0,
                actual: result.RemainingPendingSimulationTicks);
            Assert.False(result.HasRemainingBacklog);
            Assert.Equal(
                expected: 1,
                actual: handler.HandleCallCount);
            Assert.Equal(
                expected: SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(60)),
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: 0,
                actual: clock.PendingSimulationTicks);
            Assert.Empty(clock.DomainEvents);
        }

        [Fact]
        public async Task ExecuteAsync_WhenMultipleFixedStepsAreDue_ProcessesEachStepSequentially()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(speed: 120m);
            clock.ClearDomainEvents();
            SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
            var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
            SimulationAdvanceExecutor executor = CreateExecutor(
                clock: clock,
                host: host,
                handler);

            SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                simulationId: clock.SimulationId,
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SimulationAdvanceExecutionStatus.Advanced,
                actual: result.Status);
            Assert.Equal(
                expected: 2,
                actual: result.StepsProcessed);
            Assert.Equal(
                expected: 2,
                actual: handler.HandleCallCount);
            Assert.Equal(
                expected: new TickId(2),
                actual: clock.TickId);
            Assert.Equal(
                expected: SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(120)),
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: 0,
                actual: clock.PendingSimulationTicks);
        }

        [Fact]
        public async Task ExecuteAsync_WhenMaxStepCapIsReached_LeavesRemainingBacklog()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(speed: 60m);
            clock.ClearDomainEvents();
            SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
            var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
            SimulationAdvanceExecutor executor = CreateExecutor(
                clock: clock,
                host: host,
                handler);

            SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                simulationId: clock.SimulationId,
                realDelta: TimeSpan.FromSeconds(30),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SimulationAdvanceExecutionStatus.Advanced,
                actual: result.Status);
            Assert.Equal(
                expected: 10,
                actual: result.StepsProcessed);
            Assert.True(result.HasRemainingBacklog);
            Assert.Equal(
                expected: TimeSpan.FromMinutes(20)
                   .Ticks,
                actual: result.RemainingPendingSimulationTicks);
            Assert.Equal(
                expected: 10,
                actual: handler.HandleCallCount);
            Assert.Equal(
                expected: TimeSpan.FromMinutes(20)
                   .Ticks,
                actual: clock.PendingSimulationTicks);
        }

        [Fact]
        public async Task ExecuteAsync_WhenMultipleStepsAreProcessed_PassesAdvancedEventsInChronologicalOrder()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(speed: 180m);
            clock.ClearDomainEvents();
            SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
            var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
            SimulationAdvanceExecutor executor = CreateExecutor(
                clock: clock,
                host: host,
                handler);

            SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                simulationId: clock.SimulationId,
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 3,
                actual: result.StepsProcessed);
            Assert.Equal(
                expected: 3,
                actual: handler.RequestedAdvancedEvents.Count);

            for (int index = 0; index < handler.RequestedAdvancedEvents.Count; index++)
            {
                SimulationTimeAdvancedDomainEvent advancedEvent = handler.RequestedAdvancedEvents[index];
                int stepNumber = index + 1;

                Assert.Equal(
                    expected: new TickId(stepNumber),
                    actual: advancedEvent.TickId);
                Assert.Equal(
                    expected: SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(60 * index)),
                    actual: advancedEvent.From);
                Assert.Equal(
                    expected: SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(60 * stepNumber)),
                    actual: advancedEvent.To);
            }
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoMatchingScenarioHandlerExists_StillProcessesDueSteps()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(speed: 60m);
            clock.ClearDomainEvents();
            SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
            var unmatchedHandler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler
            {
                HostKind = (SimulationHostKind)999
            };
            SimulationAdvanceExecutor executor = CreateExecutor(
                clock: clock,
                host: host,
                unmatchedHandler);

            SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                simulationId: clock.SimulationId,
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SimulationAdvanceExecutionStatus.Advanced,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: result.StepsProcessed);
            Assert.Equal(
                expected: 0,
                actual: unmatchedHandler.HandleCallCount);
            Assert.Equal(
                expected: SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(60)),
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: 0,
                actual: clock.PendingSimulationTicks);
            Assert.Empty(clock.DomainEvents);
        }

        private static SimulationAdvanceExecutor CreateExecutor(
            SimulationClock clock,
            SimulationHost host,
            params SimulationTestSupport.FakeSimulationScenarioAdvanceHandler[] handlers)
        {
            var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository
            {
                ClockBySimulationId = clock
            };
            var hostRepository = new SimulationTestSupport.FakeSimulationHostReadRepository
            {
                HostBySimulationId = host
            };

            return new SimulationAdvanceExecutor(
                repository: clockRepository,
                simulationHostRepository: hostRepository,
                scenarioAdvanceHandlers: handlers,
                fixedStepSettings: new SimulationTestSupport.FakeSimulationFixedStepSettings(),
                unitOfWork: new SimulationTestSupport.FakeUnitOfWork());
        }
    }
}
