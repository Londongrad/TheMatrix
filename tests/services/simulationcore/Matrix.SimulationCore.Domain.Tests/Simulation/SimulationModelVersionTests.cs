using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation;

public sealed class SimulationModelVersionTests
{
    [Fact]
    public void Constructor_ShouldNormalizeVersion()
    {
        var version = new SimulationModelVersion("  classic-city-v1  ");

        Assert.Equal("classic-city-v1", version.Value);
        Assert.Equal("classic-city-v1", version.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldRejectEmptyVersion(string? value)
    {
        DomainException exception = Assert.Throws<DomainException>(() => new SimulationModelVersion(value));

        Assert.Equal("SimulationCore.Simulation.ModelVersion.NullOrEmpty", exception.Code);
    }

    [Fact]
    public void Constructor_ShouldRejectVersionLongerThanLimit()
    {
        DomainException exception = Assert.Throws<DomainException>(() =>
            new SimulationModelVersion(new string('v', SimulationModelVersion.MaxLength + 1)));

        Assert.Equal("SimulationCore.Simulation.ModelVersion.TooLong", exception.Code);
    }
}
