using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities;

public sealed class ScenarioModelSetVersionTests
{
    private const string NullOrEmptyErrorCode = "SimulationCore.City.ScenarioModelSetVersion.NullOrEmpty";
    private const string TooLongErrorCode = "SimulationCore.City.ScenarioModelSetVersion.TooLong";

    [Fact]
    public void Default_ReturnsExpectedDefaultValue()
    {
        var version = ScenarioModelSetVersion.Default();

        Assert.Equal(ScenarioModelSetVersion.DefaultValue, version.Value);
    }

    [Fact]
    public void Constructor_TrimsAndStoresValue()
    {
        var version = new ScenarioModelSetVersion("  classic-city-v2  ");

        Assert.Equal("classic-city-v2", version.Value);
        Assert.Equal("classic-city-v2", version.ToString());
    }

    [Fact]
    public void Constructor_WhenValueIsNull_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new ScenarioModelSetVersion(null));

        Assert.Equal(NullOrEmptyErrorCode, exception.Code);
    }

    [Fact]
    public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new ScenarioModelSetVersion("   "));

        Assert.Equal(NullOrEmptyErrorCode, exception.Code);
    }

    [Fact]
    public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new ScenarioModelSetVersion(new string('v', ScenarioModelSetVersion.MaxLength + 1)));

        Assert.Equal(TooLongErrorCode, exception.Code);
    }
}
