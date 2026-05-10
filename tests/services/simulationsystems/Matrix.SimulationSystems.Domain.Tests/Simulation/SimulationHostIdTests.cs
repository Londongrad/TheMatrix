using Matrix.SimulationSystems.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Simulation;

public sealed class SimulationHostIdTests
{
    [Fact]
    public void Constructor_WhenGuidIsEmpty_Throws()
    {
        Assert.ThrowsAny<Exception>(() => new SimulationHostId(Guid.Empty));
    }

    [Fact]
    public void New_CreatesNonEmptyIdentifier()
    {
        SimulationHostId id = SimulationHostId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
        Assert.Equal(id.Value.ToString(), id.ToString());
    }
}
