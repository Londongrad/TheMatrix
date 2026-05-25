using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceRunningSimulations;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.AdvanceRunningSimulations
{
    public sealed class AdvanceRunningSimulationsCommandHandlerTests
    {
        [Fact]
        public async Task Handle_MapsBatchAdvanceResult()
        {
            var realDelta = TimeSpan.FromMilliseconds(500);
            var executor = new SimulationTestSupport.FakeSimulationBatchAdvanceExecutor
            {
                Result = new SimulationBatchAdvanceResult(
                    ProcessedCount: 7,
                    AdvancedCount: 5,
                    NoStepDueCount: 1,
                    LaggingCount: 2,
                    FailedCount: 1,
                    TotalStepsProcessed: 12)
            };
            var handler = new AdvanceRunningSimulationsCommandHandler(executor);

            AdvanceRunningSimulationsResult result = await handler.Handle(
                request: new AdvanceRunningSimulationsCommand(realDelta),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: realDelta,
                actual: executor.RequestedRealDelta);
            Assert.Equal(
                expected: 7,
                actual: result.ProcessedCount);
            Assert.Equal(
                expected: 5,
                actual: result.AdvancedCount);
            Assert.Equal(
                expected: 1,
                actual: result.NoStepDueCount);
            Assert.Equal(
                expected: 2,
                actual: result.LaggingCount);
            Assert.Equal(
                expected: 1,
                actual: result.FailedCount);
            Assert.Equal(
                expected: 12,
                actual: result.TotalStepsProcessed);
        }
    }
}
