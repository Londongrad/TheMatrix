using Matrix.SimulationCore.Application.UseCases.Simulation.JumpClock;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.JumpClock
{
    public sealed class JumpClockCommandHandlerTests
    {
        [Fact]
        public async Task Handle_DelegatesMutationAndJumpsClockTime()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock();
            DateTimeOffset newTimeUtc = SimulationTestSupport.SimStartTimeUtc.AddHours(5);
            var executor = new SimulationTestSupport.FakeSimulationClockMutationExecutor
            {
                Clock = clock,
                Result = true
            };
            var handler = new JumpClockCommandHandler(executor);

            bool result = await handler.Handle(
                request: new JumpClockCommand(
                    SimulationId: clock.SimulationId.Value,
                    NewSimTimeUtc: newTimeUtc),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: clock.SimulationId.Value,
                actual: executor.RequestedSimulationId!.Value.Value);
            Assert.Equal(
                expected: newTimeUtc,
                actual: clock.CurrentTime.ValueUtc);
            Assert.Equal(
                expected: TickId.Start()
                   .Next()
                   .Value,
                actual: clock.TickId.Value);
        }
    }
}
