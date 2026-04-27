using Matrix.SimulationCore.Application.UseCases.Simulation.JumpClock;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.JumpClock;

public sealed class JumpClockCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesMutationAndJumpsClockTime()
    {
        var clock = SimulationTestSupport.CreateClock();
        DateTimeOffset newTimeUtc = SimulationTestSupport.SimStartTimeUtc.AddHours(5);
        var executor = new SimulationTestSupport.FakeSimulationClockMutationExecutor
        {
            Clock = clock,
            Result = true
        };
        var handler = new JumpClockCommandHandler(executor);

        var result = await handler.Handle(new JumpClockCommand(clock.SimulationId.Value, newTimeUtc), CancellationToken.None);

        Assert.True(result);
        Assert.Equal(clock.SimulationId.Value, executor.RequestedSimulationId!.Value.Value);
        Assert.Equal(newTimeUtc, clock.CurrentTime.ValueUtc);
        Assert.Equal(TickId.Start().Next().Value, clock.TickId.Value);
    }
}
