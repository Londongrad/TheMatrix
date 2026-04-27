using Matrix.SimulationCore.Application.UseCases.Simulation.SetClockSpeed;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.SetClockSpeed;

public sealed class SetClockSpeedCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesMutationAndUpdatesSpeed()
    {
        var clock = SimulationTestSupport.CreateClock(speed: 60m);
        var executor = new SimulationTestSupport.FakeSimulationClockMutationExecutor
        {
            Clock = clock,
            Result = true
        };
        var handler = new SetClockSpeedCommandHandler(executor);

        var result = await handler.Handle(new SetClockSpeedCommand(clock.SimulationId.Value, 120m), CancellationToken.None);

        Assert.True(result);
        Assert.Equal(clock.SimulationId.Value, executor.RequestedSimulationId!.Value.Value);
        Assert.Equal(120m, clock.Speed.Multiplier);
        Assert.Equal(1, clock.TickId.Value);
    }
}
