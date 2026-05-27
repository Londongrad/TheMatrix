using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Services.Simulation
{
    public sealed class SimulationAdvanceExecutorNoStepDueTests
    {
        [Fact]
        public async Task ExecuteAsync_WhenHostDoesNotExist_ReturnsNotFoundWithoutLoadingClock()
        {
            SimulationId simulationId = new(Guid.NewGuid());
            var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
            var hostRepository = new SimulationTestSupport.FakeSimulationHostReadRepository();
            var unitOfWork = new SimulationTestSupport.FakeUnitOfWork();
            var executor = new SimulationAdvanceExecutor(
                repository: clockRepository,
                simulationHostRepository: hostRepository,
                scenarioAdvanceHandlerRegistry: new SimulationScenarioAdvanceHandlerRegistry(
                    Array.Empty<ISimulationScenarioAdvanceHandler>()),
                fixedStepSettings: new SimulationTestSupport.FakeSimulationFixedStepSettings(),
                unitOfWork: unitOfWork);

            SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                simulationId: simulationId,
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SimulationAdvanceExecutionStatus.NotFound,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.StepsProcessed);
            Assert.Equal(
                expected: simulationId,
                actual: hostRepository.RequestedSimulationId);
            Assert.Null(clockRepository.RequestedSimulationId);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.ExecuteInTransactionCallCount);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
        }

        [Fact]
        public async Task ExecuteAsync_WhenClockDoesNotExist_ReturnsNotFound()
        {
            SimulationHost host = SimulationTestSupport.CreateHost();
            var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
            var hostRepository = new SimulationTestSupport.FakeSimulationHostReadRepository
            {
                HostBySimulationId = host
            };
            var unitOfWork = new SimulationTestSupport.FakeUnitOfWork();
            var executor = new SimulationAdvanceExecutor(
                repository: clockRepository,
                simulationHostRepository: hostRepository,
                scenarioAdvanceHandlerRegistry: new SimulationScenarioAdvanceHandlerRegistry(
                    Array.Empty<ISimulationScenarioAdvanceHandler>()),
                fixedStepSettings: new SimulationTestSupport.FakeSimulationFixedStepSettings(),
                unitOfWork: unitOfWork);

            SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                simulationId: host.SimulationId,
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SimulationAdvanceExecutionStatus.NotFound,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.StepsProcessed);
            Assert.Equal(
                expected: host.SimulationId,
                actual: clockRepository.RequestedSimulationId);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.ExecuteInTransactionCallCount);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
        }

        [Fact]
        public async Task ExecuteAsync_WhenAccumulatedTimeDoesNotReachFixedStep_ReturnsNoStepDue()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(speed: 30m);
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
                expected: SimulationAdvanceExecutionStatus.NoStepDue,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.StepsProcessed);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(30)
                   .Ticks,
                actual: result.RemainingPendingSimulationTicks);
            Assert.False(result.HasRemainingBacklog);
            Assert.Equal(
                expected: 0,
                actual: handler.HandleCallCount);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(30)
                   .Ticks,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc),
                actual: clock.CurrentTime);
            Assert.Empty(clock.DomainEvents);
        }

        [Fact]
        public async Task ExecuteAsync_WhenCarryOverCompletesFixedStep_ProcessesStepOnSecondExecution()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(speed: 30m);
            clock.ClearDomainEvents();
            SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
            var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
            SimulationAdvanceExecutor executor = CreateExecutor(
                clock: clock,
                host: host,
                handler);

            SimulationAdvanceExecutionResult firstResult = await executor.ExecuteAsync(
                simulationId: clock.SimulationId,
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            SimulationAdvanceExecutionResult secondResult = await executor.ExecuteAsync(
                simulationId: clock.SimulationId,
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SimulationAdvanceExecutionStatus.NoStepDue,
                actual: firstResult.Status);
            Assert.Equal(
                expected: SimulationAdvanceExecutionStatus.Advanced,
                actual: secondResult.Status);
            Assert.Equal(
                expected: 1,
                actual: secondResult.StepsProcessed);
            Assert.Equal(
                expected: 1,
                actual: handler.HandleCallCount);
            Assert.Equal(
                expected: 0,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(60)),
                actual: clock.CurrentTime);
        }

        [Fact]
        public async Task ExecuteAsync_WhenClockIsPaused_ReturnsNoStepDueAndDoesNotAccumulate()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(state: ClockState.Paused);
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
                expected: SimulationAdvanceExecutionStatus.NoStepDue,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.StepsProcessed);
            Assert.Equal(
                expected: 0,
                actual: result.RemainingPendingSimulationTicks);
            Assert.False(result.HasRemainingBacklog);
            Assert.Equal(
                expected: 0,
                actual: handler.HandleCallCount);
            Assert.Equal(
                expected: SimulationTestSupport.SimStartTimeUtc,
                actual: clock.CurrentTime.ValueUtc);
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
                scenarioAdvanceHandlerRegistry: new SimulationScenarioAdvanceHandlerRegistry(handlers),
                fixedStepSettings: new SimulationTestSupport.FakeSimulationFixedStepSettings(),
                unitOfWork: new SimulationTestSupport.FakeUnitOfWork());
        }
    }
}
