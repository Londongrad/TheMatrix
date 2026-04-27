using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceRunningSimulations;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.AdvanceRunningSimulations;

public sealed class AdvanceRunningSimulationsCommandHandlerTests
{
    [Fact]
    public async Task Handle_MapsBatchAdvanceResult()
    {
        TimeSpan realDelta = TimeSpan.FromMilliseconds(500);
        var executor = new SimulationTestSupport.FakeSimulationBatchAdvanceExecutor
        {
            Result = new SimulationBatchAdvanceResult(
                ProcessedCount: 7,
                AdvancedCount: 5,
                SkippedCount: 1,
                FailedCount: 1)
        };
        var handler = new AdvanceRunningSimulationsCommandHandler(executor);

        var result = await handler.Handle(new AdvanceRunningSimulationsCommand(realDelta), CancellationToken.None);

        Assert.Equal(realDelta, executor.RequestedRealDelta);
        Assert.Equal(7, result.ProcessedCount);
        Assert.Equal(5, result.AdvancedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(1, result.FailedCount);
    }
}
