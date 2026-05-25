using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.ValueObjects
{
    public sealed class GeoLocationTests
    {
        [Fact]
        public void Create_TrimsCountryRegionAndCity()
        {
            var geoLocation = GeoLocation.Create(
                country: "  Russia  ",
                region: "  Zabaykalsky Krai  ",
                city: "  Chita  ");

            Assert.Equal(
                expected: "Russia",
                actual: geoLocation.Country);
            Assert.Equal(
                expected: "Zabaykalsky Krai",
                actual: geoLocation.Region);
            Assert.Equal(
                expected: "Chita",
                actual: geoLocation.City);
        }

        [Fact]
        public void Create_WithWhitespaceRegionAndCity_NormalizesThemToNull()
        {
            var geoLocation = GeoLocation.Create(
                country: "Russia",
                region: "   ",
                city: "   ");

            Assert.Equal(
                expected: "Russia",
                actual: geoLocation.Country);
            Assert.Null(geoLocation.Region);
            Assert.Null(geoLocation.City);
        }

        [Fact]
        public void Create_WithWhitespaceCountry_ThrowsArgumentException()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() => GeoLocation.Create(
                country: "   ",
                region: null,
                city: null));

            Assert.Equal(
                expected: "country",
                actual: exception.ParamName);
        }
    }
}
