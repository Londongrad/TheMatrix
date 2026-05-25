using Matrix.SimulationCore.Application.UseCases.Simulation.PauseClock;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.PauseClock
{
    public sealed class PauseClockCommandHandlerTests
    {
        [Fact]
        public async Task Handle_DelegatesMutationAndPausesClock()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(state: ClockState.Running);
            var executor = new SimulationTestSupport.FakeSimulationClockMutationExecutor
            {
                Clock = clock,
                Result = true
            };
            var handler = new PauseClockCommandHandler(executor);

            bool result = await handler.Handle(
                request: new PauseClockCommand(clock.SimulationId.Value),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: clock.SimulationId.Value,
                actual: executor.RequestedSimulationId!.Value.Value);
            Assert.False(executor.RequestedAllowArchivedHost);
            Assert.Equal(
                expected: ClockState.Paused,
                actual: clock.State);
            Assert.True(clock.IsPaused);
        }
    }
}
