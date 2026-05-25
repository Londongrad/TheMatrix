using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather
{
    public sealed class WeatherStateTests
    {
        [Fact]
        public void Create_WithValidValues_CreatesWeatherState()
        {
            WeatherState state = WeatherTestData.CreateWeatherState(
                type: WeatherType.Rain,
                precipitationKind: PrecipitationKind.Rain,
                severity: WeatherSeverity.Moderate,
                temperature: TemperatureC.From(11m),
                humidity: HumidityPercent.From(80m),
                windSpeed: WindSpeedKph.From(20m),
                cloudCoverage: CloudCoveragePercent.From(90m),
                pressure: PressureHpa.From(1002m));

            Assert.Equal(
                expected: WeatherType.Rain,
                actual: state.Type);
            Assert.Equal(
                expected: WeatherSeverity.Moderate,
                actual: state.Severity);
            Assert.Equal(
                expected: PrecipitationKind.Rain,
                actual: state.PrecipitationKind);
            Assert.Equal(
                expected: TemperatureC.From(11m),
                actual: state.Temperature);
            Assert.Equal(
                expected: HumidityPercent.From(80m),
                actual: state.Humidity);
            Assert.Equal(
                expected: WindSpeedKph.From(20m),
                actual: state.WindSpeed);
            Assert.Equal(
                expected: CloudCoveragePercent.From(90m),
                actual: state.CloudCoverage);
            Assert.Equal(
                expected: PressureHpa.From(1002m),
                actual: state.Pressure);
            Assert.Equal(
                expected: WeatherTestData.StartedAt,
                actual: state.StartedAt);
            Assert.Equal(
                expected: WeatherTestData.ExpectedUntil,
                actual: state.ExpectedUntil);
        }

        [Fact]
        public void Create_WhenExpectedUntilIsNotAfterStartedAt_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => WeatherState.Create(
                type: WeatherType.Clear,
                severity: WeatherSeverity.Calm,
                precipitationKind: PrecipitationKind.None,
                temperature: TemperatureC.From(20m),
                humidity: HumidityPercent.From(40m),
                windSpeed: WindSpeedKph.From(5m),
                cloudCoverage: CloudCoveragePercent.From(10m),
                pressure: PressureHpa.From(1015m),
                startedAt: WeatherTestData.StartedAt,
                expectedUntil: WeatherTestData.StartedAt));

            Assert.Equal(
                expected: "SimulationCore.Weather.State.TimeRange.Invalid",
                actual: exception.Code);
        }

        [Fact]
        public void Create_WhenPrecipitationDoesNotMatchType_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => WeatherState.Create(
                type: WeatherType.Clear,
                severity: WeatherSeverity.Calm,
                precipitationKind: PrecipitationKind.Rain,
                temperature: TemperatureC.From(20m),
                humidity: HumidityPercent.From(40m),
                windSpeed: WindSpeedKph.From(5m),
                cloudCoverage: CloudCoveragePercent.From(10m),
                pressure: PressureHpa.From(1015m),
                startedAt: WeatherTestData.StartedAt,
                expectedUntil: WeatherTestData.ExpectedUntil));

            Assert.Equal(
                expected: "SimulationCore.Weather.Precipitation.Incoherent",
                actual: exception.Code);
        }

        [Fact]
        public void IsActiveAt_IsStartInclusive_AndEndExclusive()
        {
            WeatherState state = WeatherTestData.CreateWeatherState();

            Assert.True(state.IsActiveAt(WeatherTestData.StartedAt));
            Assert.True(state.IsActiveAt(WeatherTestData.Midpoint));
            Assert.False(state.IsActiveAt(WeatherTestData.ExpectedUntil));
        }

        [Fact]
        public void HasSameConditionsAs_WhenOnlyTimeWindowDiffers_ReturnsTrue()
        {
            WeatherState first = WeatherTestData.CreateWeatherState(
                type: WeatherType.Rain,
                precipitationKind: PrecipitationKind.Rain,
                severity: WeatherSeverity.Moderate);
            WeatherState second = WeatherTestData.CreateWeatherState(
                type: WeatherType.Rain,
                precipitationKind: PrecipitationKind.Rain,
                severity: WeatherSeverity.Moderate,
                startedAt: WeatherTestData.Midpoint,
                expectedUntil: WeatherTestData.AfterEnd);

            Assert.True(first.HasSameConditionsAs(second));
        }

        [Fact]
        public void HasSameConditionsAs_WhenConditionsDiffer_ReturnsFalse()
        {
            WeatherState first = WeatherTestData.CreateWeatherState();
            WeatherState second = WeatherTestData.CreateWeatherState(temperature: TemperatureC.From(5m));

            Assert.False(first.HasSameConditionsAs(second));
        }

        [Fact]
        public void HasSameConditionsAs_WhenOtherIsNull_ThrowsDomainException()
        {
            WeatherState state = WeatherTestData.CreateWeatherState();

            DomainException exception = Assert.Throws<DomainException>(() => state.HasSameConditionsAs(null!));

            Assert.Equal(
                expected: "Domain.Guard.Null",
                actual: exception.Code);
            Assert.Equal(
                expected: "other",
                actual: exception.PropertyName);
        }
    }
}
