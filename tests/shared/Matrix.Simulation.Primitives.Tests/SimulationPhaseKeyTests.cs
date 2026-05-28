using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Simulation.Primitives.Tests;

public sealed class SimulationPhaseKeyTests
{
    [Theory]
    [InlineData("advance-time")]
    [InlineData("resource-settlement")]
    [InlineData("tick-completed")]
    public void Constructor_ShouldAcceptCanonicalKeys(string value)
    {
        var key = new SimulationPhaseKey(value);

        Assert.Equal(value, key.Value);
        Assert.Equal(value, key.ToString());
        Assert.False(key.IsEmpty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AdvanceTime")]
    [InlineData("advance_time")]
    public void Constructor_ShouldRejectInvalidKeys(string? value)
    {
        Assert.Throws<ArgumentException>(() => new SimulationPhaseKey(value!));
    }

    [Fact]
    public void DefaultValue_ShouldBeReportedAsEmpty()
    {
        var key = default(SimulationPhaseKey);

        Assert.True(key.IsEmpty);
        Assert.Equal(string.Empty, key.ToString());
    }
}
