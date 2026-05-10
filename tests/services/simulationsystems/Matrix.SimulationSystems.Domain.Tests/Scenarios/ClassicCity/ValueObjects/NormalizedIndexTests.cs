using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.ValueObjects;

public sealed class NormalizedIndexTests
{
    [Fact]
    public void Constructors_RoundToFourDecimals()
    {
        Assert.Equal(0.1235m, new FloodingIndex(0.12345m).Value);
        Assert.Equal(0.9877m, new HeatingCoverageIndex(0.98765m).Value);
        Assert.Equal(0.5556m, new PowerCoverageIndex(0.55555m).Value);
        Assert.Equal(0.4322m, new RoadAccessibilityIndex(0.43215m).Value);
        Assert.Equal(0.2223m, new SanitationCoverageIndex(0.22225m).Value);
        Assert.Equal(0.1112m, new SnowAccumulationIndex(0.11115m).Value);
        Assert.Equal(0.7778m, new UtilityContinuityIndex(0.77775m).Value);
        Assert.Equal(0.8642m, new WaterCoverageIndex(0.86415m).Value);
    }

    [Fact]
    public void Constructors_WhenValueIsOutsideRange_Throw()
    {
        Assert.ThrowsAny<Exception>(() => new FloodingIndex(-0.01m));
        Assert.ThrowsAny<Exception>(() => new HeatingCoverageIndex(1.01m));
        Assert.ThrowsAny<Exception>(() => new PowerCoverageIndex(-0.01m));
        Assert.ThrowsAny<Exception>(() => new RoadAccessibilityIndex(1.01m));
        Assert.ThrowsAny<Exception>(() => new SanitationCoverageIndex(-0.01m));
        Assert.ThrowsAny<Exception>(() => new SnowAccumulationIndex(1.01m));
        Assert.ThrowsAny<Exception>(() => new UtilityContinuityIndex(-0.01m));
        Assert.ThrowsAny<Exception>(() => new WaterCoverageIndex(1.01m));
    }
}
