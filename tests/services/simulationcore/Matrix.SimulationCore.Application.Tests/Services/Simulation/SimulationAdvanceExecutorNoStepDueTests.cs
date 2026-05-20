using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Services.Simulation;

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
            clockRepository,
            hostRepository,
            Array.Empty<ISimulationScenarioAdvanceHandler>(),
            new SimulationTestSupport.FakeSimulationFixedStepSettings(),
            unitOfWork);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            simulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.NotFound, result.Status);
        Assert.Equal(0, result.StepsProcessed);
        Assert.Equal(simulationId, hostRepository.RequestedSimulationId);
        Assert.Null(clockRepository.RequestedSimulationId);
        Assert.Equal(0, unitOfWork.ExecuteInTransactionCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
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
            clockRepository,
            hostRepository,
            Array.Empty<ISimulationScenarioAdvanceHandler>(),
            new SimulationTestSupport.FakeSimulationFixedStepSettings(),
            unitOfWork);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            host.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.NotFound, result.Status);
        Assert.Equal(0, result.StepsProcessed);
        Assert.Equal(host.SimulationId, clockRepository.RequestedSimulationId);
        Assert.Equal(0, unitOfWork.ExecuteInTransactionCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAccumulatedTimeDoesNotReachFixedStep_ReturnsNoStepDue()
    {
        SimulationClock clock = SimulationTestSupport.CreateClock(speed: 30m);
        clock.ClearDomainEvents();
        SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
        SimulationAdvanceExecutor executor = CreateExecutor(clock, host, handler);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.NoStepDue, result.Status);
        Assert.Equal(0, result.StepsProcessed);
        Assert.Equal(TimeSpan.FromSeconds(30).Ticks, result.RemainingPendingSimulationTicks);
        Assert.False(result.HasRemainingBacklog);
        Assert.Equal(0, handler.HandleCallCount);
        Assert.Equal(TimeSpan.FromSeconds(30).Ticks, clock.PendingSimulationTicks);
        Assert.Equal(SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc), clock.CurrentTime);
        Assert.Empty(clock.DomainEvents);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCarryOverCompletesFixedStep_ProcessesStepOnSecondExecution()
    {
        SimulationClock clock = SimulationTestSupport.CreateClock(speed: 30m);
        clock.ClearDomainEvents();
        SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
        SimulationAdvanceExecutor executor = CreateExecutor(clock, host, handler);

        SimulationAdvanceExecutionResult firstResult = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        SimulationAdvanceExecutionResult secondResult = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.NoStepDue, firstResult.Status);
        Assert.Equal(SimulationAdvanceExecutionStatus.Advanced, secondResult.Status);
        Assert.Equal(1, secondResult.StepsProcessed);
        Assert.Equal(1, handler.HandleCallCount);
        Assert.Equal(0, clock.PendingSimulationTicks);
        Assert.Equal(SimTime.FromUtc(SimulationTestSupport.SimStartTimeUtc.AddSeconds(60)), clock.CurrentTime);
    }

    [Fact]
    public async Task ExecuteAsync_WhenClockIsPaused_ReturnsNoStepDueAndDoesNotAccumulate()
    {
        SimulationClock clock = SimulationTestSupport.CreateClock(state: ClockState.Paused);
        clock.ClearDomainEvents();
        SimulationHost host = SimulationTestSupport.CreateHost(clock.SimulationId.Value);
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
        SimulationAdvanceExecutor executor = CreateExecutor(clock, host, handler);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.NoStepDue, result.Status);
        Assert.Equal(0, result.StepsProcessed);
        Assert.Equal(0, result.RemainingPendingSimulationTicks);
        Assert.False(result.HasRemainingBacklog);
        Assert.Equal(0, handler.HandleCallCount);
        Assert.Equal(SimulationTestSupport.SimStartTimeUtc, clock.CurrentTime.ValueUtc);
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
