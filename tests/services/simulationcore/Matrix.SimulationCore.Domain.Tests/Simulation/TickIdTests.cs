using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation;

public sealed class TickIdTests
{
    [Fact]
    public void Start_ReturnsZero()
    {
        Assert.Equal(0, TickId.Start().Value);
    }

    [Fact]
    public void Next_IncrementsValueMonotonically()
    {
        var next = new TickId(41).Next();

        Assert.Equal(42, next.Value);
    }
}
