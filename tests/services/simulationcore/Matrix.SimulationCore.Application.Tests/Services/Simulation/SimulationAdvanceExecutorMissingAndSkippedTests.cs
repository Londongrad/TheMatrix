using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Services.Simulation;

public sealed class SimulationAdvanceExecutorMissingAndSkippedTests
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
            Array.Empty<Matrix.SimulationCore.Application.Services.Simulation.Abstractions.ISimulationScenarioAdvanceHandler>(),
            unitOfWork);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            simulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.NotFound, result.Status);
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
            Array.Empty<Matrix.SimulationCore.Application.Services.Simulation.Abstractions.ISimulationScenarioAdvanceHandler>(),
            unitOfWork);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            host.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.NotFound, result.Status);
        Assert.Equal(host.SimulationId, clockRepository.RequestedSimulationId);
        Assert.Equal(0, unitOfWork.ExecuteInTransactionCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenClockIsPaused_ReturnsSkippedAndDoesNotInvokeHandler()
    {
        SimulationClock clock = SimulationTestSupport.CreateClock(state: ClockState.Paused);
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
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
        var unitOfWork = new SimulationTestSupport.FakeUnitOfWork();
        var executor = new SimulationAdvanceExecutor(
            clockRepository,
            hostRepository,
            [handler],
            unitOfWork);

        SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
            clock.SimulationId,
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        Assert.Equal(SimulationAdvanceExecutionStatus.Skipped, result.Status);
        Assert.Equal(1, unitOfWork.ExecuteInTransactionCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(0, handler.HandleCallCount);
        Assert.Equal(SimulationTestSupport.SimStartTimeUtc, clock.CurrentTime.ValueUtc);
        Assert.Empty(clock.DomainEvents);
    }
}
