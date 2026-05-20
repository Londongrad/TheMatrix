using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Services.Simulation;

public sealed class SimulationAdvanceExecutorAdvancedTests
{
    [Fact]
    public async Task ExecuteAsync_WhenOneFixedStepIsDue_AdvancesClockAndDispatchesOneStep()
    {
        SimulationClock clock = SimulationTestSupport.CreateClock(speed: 60m);
        clock.ClearDomainEvents();
        SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
        SimulationAdvanceExecutor executor = CreateExecutor(clock, host, handler);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.Advanced, result.Status);
        Assert.Equal(1, result.StepsProcessed);
        Assert.Equal(0, result.RemainingPendingSimulationTicks);
        Assert.False(result.HasRemainingBacklog);
        Assert.Equal(1, handler.HandleCallCount);
        Assert.Equal(SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(60)), clock.CurrentTime);
        Assert.Equal(0, clock.PendingSimulationTicks);
        Assert.Empty(clock.DomainEvents);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultipleFixedStepsAreDue_ProcessesEachStepSequentially()
    {
        SimulationClock clock = SimulationTestSupport.CreateClock(speed: 120m);
        clock.ClearDomainEvents();
        SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
        SimulationAdvanceExecutor executor = CreateExecutor(clock, host, handler);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.Advanced, result.Status);
        Assert.Equal(2, result.StepsProcessed);
        Assert.Equal(2, handler.HandleCallCount);
        Assert.Equal(new TickId(2), clock.TickId);
        Assert.Equal(SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(120)), clock.CurrentTime);
        Assert.Equal(0, clock.PendingSimulationTicks);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMaxStepCapIsReached_LeavesRemainingBacklog()
    {
        SimulationClock clock = SimulationTestSupport.CreateClock(speed: 60m);
        clock.ClearDomainEvents();
        SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
        SimulationAdvanceExecutor executor = CreateExecutor(clock, host, handler);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.Advanced, result.Status);
        Assert.Equal(10, result.StepsProcessed);
        Assert.True(result.HasRemainingBacklog);
        Assert.Equal(TimeSpan.FromMinutes(20).Ticks, result.RemainingPendingSimulationTicks);
        Assert.Equal(10, handler.HandleCallCount);
        Assert.Equal(TimeSpan.FromMinutes(20).Ticks, clock.PendingSimulationTicks);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultipleStepsAreProcessed_PassesAdvancedEventsInChronologicalOrder()
    {
        SimulationClock clock = SimulationTestSupport.CreateClock(speed: 180m);
        clock.ClearDomainEvents();
        SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
        SimulationAdvanceExecutor executor = CreateExecutor(clock, host, handler);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(3, result.StepsProcessed);
        Assert.Equal(3, handler.RequestedAdvancedEvents.Count);

        for (int index = 0; index < handler.RequestedAdvancedEvents.Count; index++)
        {
            SimulationTimeAdvancedDomainEvent advancedEvent = handler.RequestedAdvancedEvents[index];
            int stepNumber = index + 1;

            Assert.Equal(new TickId(stepNumber), advancedEvent.TickId);
            Assert.Equal(SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(60 * index)), advancedEvent.From);
            Assert.Equal(SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(60 * stepNumber)), advancedEvent.To);
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
        SimulationAdvanceExecutor executor = CreateExecutor(clock, host, unmatchedHandler);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.Advanced, result.Status);
        Assert.Equal(1, result.StepsProcessed);
        Assert.Equal(0, unmatchedHandler.HandleCallCount);
        Assert.Equal(SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(60)), clock.CurrentTime);
        Assert.Equal(0, clock.PendingSimulationTicks);
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
            clockRepository,
            hostRepository,
            handlers,
            new SimulationTestSupport.FakeSimulationFixedStepSettings(),
            new SimulationTestSupport.FakeUnitOfWork());
    }
}
