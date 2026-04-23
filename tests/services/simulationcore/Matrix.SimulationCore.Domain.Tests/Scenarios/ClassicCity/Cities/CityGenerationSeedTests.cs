using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities;

public sealed class CityGenerationSeedTests
{
    private const string NullOrEmptyErrorCode = "SimulationCore.City.GenerationSeed.NullOrEmpty";
    private const string TooLongErrorCode = "SimulationCore.City.GenerationSeed.TooLong";

    [Fact]
    public void Constructor_TrimsAndStoresValue()
    {
        var seed = new CityGenerationSeed("  seed-42  ");

        Assert.Equal("seed-42", seed.Value);
        Assert.Equal("seed-42", seed.ToString());
    }

    [Fact]
    public void Constructor_WhenValueIsNull_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new CityGenerationSeed(null));

        Assert.Equal(NullOrEmptyErrorCode, exception.Code);
    }

    [Fact]
    public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new CityGenerationSeed("   "));

        Assert.Equal(NullOrEmptyErrorCode, exception.Code);
    }

    [Fact]
    public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new CityGenerationSeed(new string('s', CityGenerationSeed.MaxLength + 1)));

        Assert.Equal(TooLongErrorCode, exception.Code);
    }
}
