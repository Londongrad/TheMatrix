using Matrix.SimulationCore.Application.UseCases.Simulation.ResumeClock;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.ResumeClock;

public sealed class ResumeClockCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesMutationAndResumesClock()
    {
        var clock = SimulationTestSupport.CreateClock(state: ClockState.Paused);
        var executor = new SimulationTestSupport.FakeSimulationClockMutationExecutor
        {
            Clock = clock,
            Result = true
        };
        var handler = new ResumeClockCommandHandler(executor);

        var result = await handler.Handle(new ResumeClockCommand(clock.SimulationId.Value), CancellationToken.None);

        Assert.True(result);
        Assert.Equal(clock.SimulationId.Value, executor.RequestedSimulationId!.Value.Value);
        Assert.False(executor.RequestedAllowArchivedHost);
        Assert.Equal(ClockState.Running, clock.State);
        Assert.False(clock.IsPaused);
    }
}
