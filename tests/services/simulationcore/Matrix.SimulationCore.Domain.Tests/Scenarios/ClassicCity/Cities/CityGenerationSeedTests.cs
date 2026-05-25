using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class CityGenerationSeedTests
    {
        private const string NullOrEmptyErrorCode = "SimulationCore.City.GenerationSeed.NullOrEmpty";
        private const string TooLongErrorCode = "SimulationCore.City.GenerationSeed.TooLong";

        [Fact]
        public void Constructor_TrimsAndStoresValue()
        {
            var seed = new CityGenerationSeed("  seed-42  ");

            Assert.Equal(
                expected: "seed-42",
                actual: seed.Value);
            Assert.Equal(
                expected: "seed-42",
                actual: seed.ToString());
        }

        [Fact]
        public void Constructor_WhenValueIsNull_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityGenerationSeed(null));

            Assert.Equal(
                expected: NullOrEmptyErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityGenerationSeed("   "));

            Assert.Equal(
                expected: NullOrEmptyErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() =>
                new CityGenerationSeed(
                    new string(
                        c: 's',
                        count: CityGenerationSeed.MaxLength + 1)));

            Assert.Equal(
                expected: TooLongErrorCode,
                actual: exception.Code);
        }
    }
}
