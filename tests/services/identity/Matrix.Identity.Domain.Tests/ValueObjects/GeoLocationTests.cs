using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.ValueObjects;

public sealed class GeoLocationTests
{
    [Fact]
    public void Create_TrimsCountryRegionAndCity()
    {
        var geoLocation = GeoLocation.Create(
            country: "  Russia  ",
            region: "  Zabaykalsky Krai  ",
            city: "  Chita  ");

        Assert.Equal("Russia", geoLocation.Country);
        Assert.Equal("Zabaykalsky Krai", geoLocation.Region);
        Assert.Equal("Chita", geoLocation.City);
    }

    [Fact]
    public void Create_WithWhitespaceRegionAndCity_NormalizesThemToNull()
    {
        var geoLocation = GeoLocation.Create(
            country: "Russia",
            region: "   ",
            city: "   ");

        Assert.Equal("Russia", geoLocation.Country);
        Assert.Null(geoLocation.Region);
        Assert.Null(geoLocation.City);
    }

    [Fact]
    public void Create_WithWhitespaceCountry_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => GeoLocation.Create(
            country: "   ",
            region: null,
            city: null));

        Assert.Equal("country", exception.ParamName);
    }
}
