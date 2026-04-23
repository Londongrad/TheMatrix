using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities;

public sealed class CityNameTests
{
    private const string NullOrEmptyErrorCode = "SimulationCore.City.Name.NullOrEmpty";
    private const string TooLongErrorCode = "SimulationCore.City.Name.TooLong";

    [Fact]
    public void Constructor_TrimsAndStoresValue()
    {
        var cityName = new CityName("  Neo Tokyo  ");

        Assert.Equal("Neo Tokyo", cityName.Value);
        Assert.Equal("Neo Tokyo", cityName.ToString());
    }

    [Fact]
    public void Constructor_WhenValueIsNull_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new CityName(null));

        Assert.Equal(NullOrEmptyErrorCode, exception.Code);
    }

    [Fact]
    public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new CityName("   "));

        Assert.Equal(NullOrEmptyErrorCode, exception.Code);
    }

    [Fact]
    public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new CityName(new string('a', CityName.MaxLength + 1)));

        Assert.Equal(TooLongErrorCode, exception.Code);
    }
}
