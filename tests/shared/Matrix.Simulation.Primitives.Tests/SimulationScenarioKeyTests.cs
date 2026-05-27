using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Simulation.Primitives.Tests;

public sealed class SimulationScenarioKeyTests
{
    [Theory]
    [InlineData("classic-city")]
    [InlineData("metro")]
    [InlineData("metro-2033")]
    public void Constructor_ShouldAcceptCanonicalKeys(string value)
    {
        var key = new SimulationScenarioKey(value);

        Assert.Equal(value, key.Value);
        Assert.Equal(value, key.ToString());
        Assert.False(key.IsEmpty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldRejectEmptyKeys(string? value)
    {
        Assert.Throws<ArgumentException>(() => new SimulationScenarioKey(value!));
    }

    [Theory]
    [InlineData("ClassicCity")]
    [InlineData("classic_city")]
    [InlineData("-classic-city")]
    [InlineData("classic-city-")]
    [InlineData("classic--city")]
    [InlineData("1-classic-city")]
    public void Constructor_ShouldRejectNonCanonicalKeys(string value)
    {
        Assert.Throws<ArgumentException>(() => new SimulationScenarioKey(value));
    }

    [Fact]
    public void Constructor_ShouldRejectKeysLongerThanLimit()
    {
        string value = $"a{new string('b', SimulationScenarioKey.MaxLength)}";

        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationScenarioKey(value));
    }

    [Fact]
    public void DefaultValue_ShouldBeReportedAsEmpty()
    {
        var key = default(SimulationScenarioKey);

        Assert.True(key.IsEmpty);
        Assert.Equal(string.Empty, key.ToString());
    }
}
