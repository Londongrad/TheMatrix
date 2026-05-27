using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Simulation.Primitives.Tests;

public sealed class SimulationRuntimeKeyTests
{
    private static readonly SimulationScenarioKey ClassicCity = new("classic-city");
    private static readonly SimulationHostTypeKey City = new("city");

    [Fact]
    public void Constructor_ShouldComposeScenarioAndHostType()
    {
        var key = new SimulationRuntimeKey(ClassicCity, City);

        Assert.Equal(ClassicCity, key.ScenarioKey);
        Assert.Equal(City, key.HostTypeKey);
        Assert.Equal("classic-city:city", key.ToString());
        Assert.False(key.IsEmpty);
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyScenarioKey()
    {
        Assert.Throws<ArgumentException>(() =>
            new SimulationRuntimeKey(default, City));
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyHostTypeKey()
    {
        Assert.Throws<ArgumentException>(() =>
            new SimulationRuntimeKey(ClassicCity, default));
    }

    [Fact]
    public void Equality_ShouldIncludeScenarioAndHostType()
    {
        var classicCity = new SimulationRuntimeKey(ClassicCity, City);
        var metroCity = new SimulationRuntimeKey(new SimulationScenarioKey("metro"), City);

        Assert.NotEqual(classicCity, metroCity);
    }
}
