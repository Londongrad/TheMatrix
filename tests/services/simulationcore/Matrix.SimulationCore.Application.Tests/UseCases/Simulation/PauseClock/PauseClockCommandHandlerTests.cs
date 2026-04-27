using Matrix.SimulationCore.Application.UseCases.Simulation.PauseClock;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.PauseClock;

public sealed class PauseClockCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesMutationAndPausesClock()
    {
        var clock = SimulationTestSupport.CreateClock(state: ClockState.Running);
        var executor = new SimulationTestSupport.FakeSimulationClockMutationExecutor
        {
            Clock = clock,
            Result = true
        };
        var handler = new PauseClockCommandHandler(executor);

        var result = await handler.Handle(new PauseClockCommand(clock.SimulationId.Value), CancellationToken.None);

        Assert.True(result);
        Assert.Equal(clock.SimulationId.Value, executor.RequestedSimulationId!.Value.Value);
        Assert.False(executor.RequestedAllowArchivedHost);
        Assert.Equal(ClockState.Paused, clock.State);
        Assert.True(clock.IsPaused);
    }
}
