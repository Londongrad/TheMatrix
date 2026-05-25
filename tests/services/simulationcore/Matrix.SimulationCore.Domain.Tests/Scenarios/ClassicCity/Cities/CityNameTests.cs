using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class CityNameTests
    {
        private const string NullOrEmptyErrorCode = "SimulationCore.City.Name.NullOrEmpty";
        private const string TooLongErrorCode = "SimulationCore.City.Name.TooLong";

        [Fact]
        public void Constructor_TrimsAndStoresValue()
        {
            var cityName = new CityName("  Neo Tokyo  ");

            Assert.Equal(
                expected: "Neo Tokyo",
                actual: cityName.Value);
            Assert.Equal(
                expected: "Neo Tokyo",
                actual: cityName.ToString());
        }

        [Fact]
        public void Constructor_WhenValueIsNull_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityName(null));

            Assert.Equal(
                expected: NullOrEmptyErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityName("   "));

            Assert.Equal(
                expected: NullOrEmptyErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityName(
                new string(
                    c: 'a',
                    count: CityName.MaxLength + 1)));

            Assert.Equal(
                expected: TooLongErrorCode,
                actual: exception.Code);
        }
    }
}
