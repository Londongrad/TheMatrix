using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather
{
    public sealed class WeatherOverrideTests
    {
        [Fact]
        public void Create_WithValidValues_CreatesOverride_AndTrimsReason()
        {
            WeatherState forcedState = WeatherTestData.CreateWeatherState(
                type: WeatherType.Storm,
                precipitationKind: PrecipitationKind.Hail,
                severity: WeatherSeverity.Severe);

            var weatherOverride = WeatherOverride.Create(
                forcedState: forcedState,
                source: WeatherOverrideSource.Manual,
                reason: "  operator request  ");

            Assert.NotEqual(
                expected: Guid.Empty,
                actual: weatherOverride.Id);
            Assert.Equal(
                expected: forcedState,
                actual: weatherOverride.ForcedState);
            Assert.Equal(
                expected: WeatherOverrideSource.Manual,
                actual: weatherOverride.Source);
            Assert.Equal(
                expected: "operator request",
                actual: weatherOverride.Reason);
            Assert.Equal(
                expected: forcedState.StartedAt,
                actual: weatherOverride.StartsAt);
            Assert.Equal(
                expected: forcedState.ExpectedUntil,
                actual: weatherOverride.EndsAt);
        }

        [Fact]
        public void Create_WithWhitespaceReason_NormalizesToNull()
        {
            var weatherOverride = WeatherOverride.Create(
                forcedState: WeatherTestData.CreateWeatherState(),
                source: WeatherOverrideSource.Debug,
                reason: "   ");

            Assert.Null(weatherOverride.Reason);
        }

        [Fact]
        public void Create_WithNullForcedState_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => WeatherOverride.Create(
                forcedState: null!,
                source: WeatherOverrideSource.Manual));

            Assert.Equal(
                expected: "Domain.Guard.Null",
                actual: exception.Code);
            Assert.Equal(
                expected: "forcedState",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithInvalidSource_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => WeatherOverride.Create(
                forcedState: WeatherTestData.CreateWeatherState(),
                source: (WeatherOverrideSource)999));

            Assert.Equal(
                expected: "Domain.Guard.InvalidEnum",
                actual: exception.Code);
            Assert.Equal(
                expected: "Source",
                actual: exception.PropertyName);
        }

        [Fact]
        public void IsActiveAt_AndHasExpiredBy_UseExpectedWindowBoundaries()
        {
            var weatherOverride = WeatherOverride.Create(
                forcedState: WeatherTestData.CreateWeatherState(),
                source: WeatherOverrideSource.System);

            Assert.True(weatherOverride.IsActiveAt(WeatherTestData.StartedAt));
            Assert.True(weatherOverride.IsActiveAt(WeatherTestData.Midpoint));
            Assert.False(weatherOverride.IsActiveAt(WeatherTestData.ExpectedUntil));

            Assert.False(weatherOverride.HasExpiredBy(WeatherTestData.Midpoint));
            Assert.True(weatherOverride.HasExpiredBy(WeatherTestData.ExpectedUntil));
            Assert.True(weatherOverride.HasExpiredBy(WeatherTestData.AfterEnd));
        }
    }
}
