using Matrix.SimulationCore.Application.UseCases.Simulation.SetClockSpeed;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.SetClockSpeed
{
    public sealed class SetClockSpeedCommandHandlerTests
    {
        [Fact]
        public async Task Handle_DelegatesMutationAndUpdatesSpeed()
        {
            SimulationClock clock = SimulationTestSupport.CreateClock(speed: 60m);
            var executor = new SimulationTestSupport.FakeSimulationClockMutationExecutor
            {
                Clock = clock,
                Result = true
            };
            var handler = new SetClockSpeedCommandHandler(executor);

            bool result = await handler.Handle(
                request: new SetClockSpeedCommand(
                    SimulationId: clock.SimulationId.Value,
                    Multiplier: 120m),
                cancellationToken: CancellationToken.None);

            Assert.True(result);
            Assert.Equal(
                expected: clock.SimulationId.Value,
                actual: executor.RequestedSimulationId!.Value.Value);
            Assert.Equal(
                expected: 120m,
                actual: clock.Speed.Multiplier);
            Assert.Equal(
                expected: 1,
                actual: clock.TickId.Value);
        }
    }
}
