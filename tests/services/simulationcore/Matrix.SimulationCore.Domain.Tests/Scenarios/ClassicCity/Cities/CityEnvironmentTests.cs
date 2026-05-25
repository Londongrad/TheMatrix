using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class CityEnvironmentTests
    {
        private const string InvalidEnumErrorCode = "Domain.Guard.InvalidEnum";

        [Fact]
        public void Create_WithValidValues_CreatesEnvironment()
        {
            var utcOffset = CityUtcOffset.FromMinutes(180);

            var environment = CityEnvironment.Create(
                climateZone: ClimateZone.Temperate,
                hemisphere: Hemisphere.Northern,
                utcOffset: utcOffset);

            Assert.Equal(
                expected: ClimateZone.Temperate,
                actual: environment.ClimateZone);
            Assert.Equal(
                expected: Hemisphere.Northern,
                actual: environment.Hemisphere);
            Assert.Equal(
                expected: utcOffset,
                actual: environment.UtcOffset);
        }

        [Fact]
        public void Create_WithInvalidClimateZone_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityEnvironment.Create(
                climateZone: (ClimateZone)999,
                hemisphere: Hemisphere.Northern,
                utcOffset: CityUtcOffset.FromMinutes(180)));

            Assert.Equal(
                expected: InvalidEnumErrorCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "ClimateZone",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithInvalidHemisphere_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityEnvironment.Create(
                climateZone: ClimateZone.Temperate,
                hemisphere: (Hemisphere)999,
                utcOffset: CityUtcOffset.FromMinutes(180)));

            Assert.Equal(
                expected: InvalidEnumErrorCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "Hemisphere",
                actual: exception.PropertyName);
        }
    }
}
