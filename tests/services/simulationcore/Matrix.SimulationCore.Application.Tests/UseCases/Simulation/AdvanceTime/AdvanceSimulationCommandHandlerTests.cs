using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceTime;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.AdvanceTime
{
    public sealed class AdvanceSimulationCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenExecutorFindsSimulation_ReturnsTrue()
        {
            var simulationId = Guid.NewGuid();
            var realDelta = TimeSpan.FromSeconds(2);
            var executor = new SimulationTestSupport.FakeSimulationAdvanceExecutor
            {
                Result = new SimulationAdvanceExecutionResult(
                    SimulationId: new SimulationId(simulationId),
                    Status: SimulationAdvanceExecutionStatus.Advanced)
            };
            var handler = new AdvanceSimulationCommandHandler(executor);

            bool result = await handler.Handle(
                request: new AdvanceSimulationCommand(
                    SimulationId: simulationId,
                    RealDelta: realDelta),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: simulationId,
                actual: executor.RequestedSimulationId!.Value.Value);
            Assert.Equal(
                expected: realDelta,
                actual: executor.RequestedRealDelta);
        }

        [Fact]
        public async Task Handle_WhenExecutorReturnsNotFound_ReturnsFalse()
        {
            var simulationId = Guid.NewGuid();
            var executor = new SimulationTestSupport.FakeSimulationAdvanceExecutor
            {
                Result = new SimulationAdvanceExecutionResult(
                    SimulationId: new SimulationId(simulationId),
                    Status: SimulationAdvanceExecutionStatus.NotFound)
            };
            var handler = new AdvanceSimulationCommandHandler(executor);

            bool result = await handler.Handle(
                request: new AdvanceSimulationCommand(
                    SimulationId: simulationId,
                    RealDelta: TimeSpan.FromSeconds(1)),
                cancellationToken: CancellationToken.None);

            Assert.False(result);
        }
    }
}
