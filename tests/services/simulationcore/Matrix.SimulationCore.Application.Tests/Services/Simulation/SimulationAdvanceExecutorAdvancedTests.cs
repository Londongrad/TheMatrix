using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Services.Simulation;

public sealed class SimulationAdvanceExecutorAdvancedTests
{
    [Fact]
    public async Task ExecuteAsync_WhenMatchingScenarioHandlerExists_AdvancesClockAndDispatchesEvent()
    {
        SimulationClock clock = SimulationTestSupport.CreateClock(speed: 60m);
        clock.ClearDomainEvents();
        SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository
        {
            ClockBySimulationId = clock
        };
        var hostRepository = new SimulationTestSupport.FakeSimulationHostReadRepository
        {
            HostBySimulationId = host
        };
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler
        {
            HostKind = SimulationHostKind.City
        };
        var unitOfWork = new SimulationTestSupport.FakeUnitOfWork();
        var executor = new SimulationAdvanceExecutor(
            clockRepository,
            hostRepository,
            [handler],
            unitOfWork);
        TimeSpan realDelta = TimeSpan.FromSeconds(2);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            clock.SimulationId,
            realDelta,
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.Advanced, result.Status);
        Assert.Equal(1, unitOfWork.ExecuteInTransactionCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(1, handler.HandleCallCount);
        Assert.Equal(host, handler.RequestedHost);
        SimulationTimeAdvancedDomainEvent advancedEvent = Assert.IsType<SimulationTimeAdvancedDomainEvent>(handler.RequestedAdvancedEvent);
        Assert.Equal(clock.SimulationId, advancedEvent.SimulationId);
        Assert.Equal(new TickId(1), advancedEvent.TickId);
        Assert.Equal(SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc), advancedEvent.From);
        Assert.Equal(SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddMinutes(2)), advancedEvent.To);
        Assert.Equal(advancedEvent.To, clock.CurrentTime);
        Assert.Empty(clock.DomainEvents);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoMatchingScenarioHandlerExists_StillReturnsAdvanced()
    {
        SimulationClock clock = SimulationTestSupport.CreateClock(speed: 30m);
        clock.ClearDomainEvents();
        SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository
        {
            ClockBySimulationId = clock
        };
        var hostRepository = new SimulationTestSupport.FakeSimulationHostReadRepository
        {
            HostBySimulationId = host
        };
        var unmatchedHandler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler
        {
            HostKind = (SimulationHostKind)999
        };
        var unitOfWork = new SimulationTestSupport.FakeUnitOfWork();
        var executor = new SimulationAdvanceExecutor(
            clockRepository,
            hostRepository,
            [unmatchedHandler],
            unitOfWork);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(2),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.Advanced, result.Status);
        Assert.Equal(0, unmatchedHandler.HandleCallCount);
        Assert.Equal(SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddMinutes(1)), clock.CurrentTime);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Empty(clock.DomainEvents);
    }
}
