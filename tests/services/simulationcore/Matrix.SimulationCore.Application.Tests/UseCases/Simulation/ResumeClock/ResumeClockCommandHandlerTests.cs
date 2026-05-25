using Matrix.SimulationCore.Application.UseCases.Simulation.ResumeClock;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.ResumeClock
{
    public sealed class ResumeClockCommandHandlerTests
    {
        [Fact]
        public async Task Handle_DelegatesMutationAndResumesClock()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(state: ClockState.Paused);
            var executor = new SimulationTestSupport.FakeSimulationClockMutationExecutor
            {
                Clock = clock,
                Result = true
            };
            var handler = new ResumeClockCommandHandler(executor);

            bool result = await handler.Handle(
                request: new ResumeClockCommand(clock.SimulationId.Value),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: clock.SimulationId.Value,
                actual: executor.RequestedSimulationId!.Value.Value);
            Assert.False(executor.RequestedAllowArchivedHost);
            Assert.Equal(
                expected: ClockState.Running,
                actual: clock.State);
            Assert.False(clock.IsPaused);
        }
    }
}
