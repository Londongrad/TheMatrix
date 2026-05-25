using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class ScenarioModelSetVersionTests
    {
        private const string NullOrEmptyErrorCode = "SimulationCore.City.ScenarioModelSetVersion.NullOrEmpty";
        private const string TooLongErrorCode = "SimulationCore.City.ScenarioModelSetVersion.TooLong";

        [Fact]
        public void Default_ReturnsExpectedDefaultValue()
        {
            var version = ScenarioModelSetVersion.Default();

            Assert.Equal(
                expected: ScenarioModelSetVersion.DefaultValue,
                actual: version.Value);
        }

        [Fact]
        public void Constructor_TrimsAndStoresValue()
        {
            var version = new ScenarioModelSetVersion("  classic-city-v2  ");

            Assert.Equal(
                expected: "classic-city-v2",
                actual: version.Value);
            Assert.Equal(
                expected: "classic-city-v2",
                actual: version.ToString());
        }

        [Fact]
        public void Constructor_WhenValueIsNull_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new ScenarioModelSetVersion(null));

            Assert.Equal(
                expected: NullOrEmptyErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new ScenarioModelSetVersion("   "));

            Assert.Equal(
                expected: NullOrEmptyErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() =>
                new ScenarioModelSetVersion(
                    new string(
                        c: 'v',
                        count: ScenarioModelSetVersion.MaxLength + 1)));

            Assert.Equal(
                expected: TooLongErrorCode,
                actual: exception.Code);
        }
    }
}
