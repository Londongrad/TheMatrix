using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Simulation.Primitives.Tests;

public sealed class SimulationHostTypeKeyTests
{
    [Theory]
    [InlineData("city")]
    [InlineData("metro-network")]
    public void Constructor_ShouldAcceptCanonicalKeys(string value)
    {
        var key = new SimulationHostTypeKey(value);

        Assert.Equal(value, key.Value);
        Assert.Equal(value, key.ToString());
        Assert.False(key.IsEmpty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("City")]
    [InlineData("metro_network")]
    public void Constructor_ShouldRejectInvalidKeys(string? value)
    {
        Assert.Throws<ArgumentException>(() => new SimulationHostTypeKey(value!));
    }
}
