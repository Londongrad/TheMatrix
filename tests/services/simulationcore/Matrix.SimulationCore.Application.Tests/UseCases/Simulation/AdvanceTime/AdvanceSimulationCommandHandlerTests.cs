using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceTime;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.AdvanceTime;

public sealed class AdvanceSimulationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenExecutorFindsSimulation_ReturnsTrue()
    {
        Guid simulationId = Guid.NewGuid();
        TimeSpan realDelta = TimeSpan.FromSeconds(2);
        var executor = new SimulationTestSupport.FakeSimulationAdvanceExecutor
        {
            Result = new SimulationAdvanceExecutionResult(
                new SimulationId(simulationId),
                SimulationAdvanceExecutionStatus.Advanced)
        };
        var handler = new AdvanceSimulationCommandHandler(executor);

        var result = await handler.Handle(new AdvanceSimulationCommand(simulationId, realDelta), CancellationToken.None);

        Assert.True(result);
        Assert.Equal(simulationId, executor.RequestedSimulationId!.Value.Value);
        Assert.Equal(realDelta, executor.RequestedRealDelta);
    }

    [Fact]
    public async Task Handle_WhenExecutorReturnsNotFound_ReturnsFalse()
    {
        Guid simulationId = Guid.NewGuid();
        var executor = new SimulationTestSupport.FakeSimulationAdvanceExecutor
        {
            Result = new SimulationAdvanceExecutionResult(
                new SimulationId(simulationId),
                SimulationAdvanceExecutionStatus.NotFound)
        };
        var handler = new AdvanceSimulationCommandHandler(executor);

        var result = await handler.Handle(new AdvanceSimulationCommand(simulationId, TimeSpan.FromSeconds(1)), CancellationToken.None);

        Assert.False(result);
    }
}
