using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation;

public sealed class SimulationSeedTests
{
    [Fact]
    public void Constructor_ShouldNormalizeSeed()
    {
        var seed = new SimulationSeed("  seed-42  ");

        Assert.Equal("seed-42", seed.Value);
        Assert.Equal("seed-42", seed.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldRejectEmptySeed(string? value)
    {
        DomainException exception = Assert.Throws<DomainException>(() => new SimulationSeed(value));

        Assert.Equal("SimulationCore.Simulation.Seed.NullOrEmpty", exception.Code);
    }

    [Fact]
    public void Constructor_ShouldRejectSeedLongerThanLimit()
    {
        DomainException exception = Assert.Throws<DomainException>(() =>
            new SimulationSeed(new string('s', SimulationSeed.MaxLength + 1)));

        Assert.Equal("SimulationCore.Simulation.Seed.TooLong", exception.Code);
    }
}
