using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class CityInitialWeatherProfileTests
    {
        private const string InvalidEnumErrorCode = "Domain.Guard.InvalidEnum";

        [Fact]
        public void CreateRandom_SetsRandomModeWithoutManualValues()
        {
            var profile = CityInitialWeatherProfile.CreateRandom();

            Assert.Equal(
                expected: InitialWeatherMode.Random,
                actual: profile.Mode);
            Assert.Null(profile.ManualType);
            Assert.Null(profile.ManualSeverity);
            Assert.Null(profile.ManualTemperature);
        }

        [Fact]
        public void CreateManual_SetsManualModeAndValues()
        {
            var temperature = TemperatureC.From(12m);

            var profile = CityInitialWeatherProfile.CreateManual(
                manualType: WeatherType.Rain,
                manualSeverity: WeatherSeverity.Moderate,
                manualTemperature: temperature);

            Assert.Equal(
                expected: InitialWeatherMode.Manual,
                actual: profile.Mode);
            Assert.Equal(
                expected: WeatherType.Rain,
                actual: profile.ManualType);
            Assert.Equal(
                expected: WeatherSeverity.Moderate,
                actual: profile.ManualSeverity);
            Assert.Equal(
                expected: temperature,
                actual: profile.ManualTemperature);
        }

        [Fact]
        public void CreateManual_WithInvalidWeatherType_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityInitialWeatherProfile.CreateManual(
                manualType: (WeatherType)999,
                manualSeverity: WeatherSeverity.Moderate));

            Assert.Equal(
                expected: InvalidEnumErrorCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "manualType",
                actual: exception.PropertyName);
        }

        [Fact]
        public void CreateManual_WithInvalidWeatherSeverity_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityInitialWeatherProfile.CreateManual(
                manualType: WeatherType.Rain,
                manualSeverity: (WeatherSeverity)999));

            Assert.Equal(
                expected: InvalidEnumErrorCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "manualSeverity",
                actual: exception.PropertyName);
        }
    }
}
