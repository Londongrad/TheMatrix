using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather;

public sealed class WeatherStateTests
{
    [Fact]
    public void Create_WithValidValues_CreatesWeatherState()
    {
        var state = WeatherTestData.CreateWeatherState(
            type: WeatherType.Rain,
            precipitationKind: PrecipitationKind.Rain,
            severity: WeatherSeverity.Moderate,
            temperature: TemperatureC.From(11m),
            humidity: HumidityPercent.From(80m),
            windSpeed: WindSpeedKph.From(20m),
            cloudCoverage: CloudCoveragePercent.From(90m),
            pressure: PressureHpa.From(1002m));

        Assert.Equal(WeatherType.Rain, state.Type);
        Assert.Equal(WeatherSeverity.Moderate, state.Severity);
        Assert.Equal(PrecipitationKind.Rain, state.PrecipitationKind);
        Assert.Equal(TemperatureC.From(11m), state.Temperature);
        Assert.Equal(HumidityPercent.From(80m), state.Humidity);
        Assert.Equal(WindSpeedKph.From(20m), state.WindSpeed);
        Assert.Equal(CloudCoveragePercent.From(90m), state.CloudCoverage);
        Assert.Equal(PressureHpa.From(1002m), state.Pressure);
        Assert.Equal(WeatherTestData.StartedAt, state.StartedAt);
        Assert.Equal(WeatherTestData.ExpectedUntil, state.ExpectedUntil);
    }

    [Fact]
    public void Create_WhenExpectedUntilIsNotAfterStartedAt_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => WeatherState.Create(
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

        Assert.Equal("SimulationCore.Weather.State.TimeRange.Invalid", exception.Code);
    }

    [Fact]
    public void Create_WhenPrecipitationDoesNotMatchType_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => WeatherState.Create(
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

        Assert.Equal("SimulationCore.Weather.Precipitation.Incoherent", exception.Code);
    }

    [Fact]
    public void IsActiveAt_IsStartInclusive_AndEndExclusive()
    {
        var state = WeatherTestData.CreateWeatherState();

        Assert.True(state.IsActiveAt(WeatherTestData.StartedAt));
        Assert.True(state.IsActiveAt(WeatherTestData.Midpoint));
        Assert.False(state.IsActiveAt(WeatherTestData.ExpectedUntil));
    }

    [Fact]
    public void HasSameConditionsAs_WhenOnlyTimeWindowDiffers_ReturnsTrue()
    {
        var first = WeatherTestData.CreateWeatherState(
            type: WeatherType.Rain,
            precipitationKind: PrecipitationKind.Rain,
            severity: WeatherSeverity.Moderate);
        var second = WeatherTestData.CreateWeatherState(
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
        var first = WeatherTestData.CreateWeatherState();
        var second = WeatherTestData.CreateWeatherState(temperature: TemperatureC.From(5m));

        Assert.False(first.HasSameConditionsAs(second));
    }

    [Fact]
    public void HasSameConditionsAs_WhenOtherIsNull_ThrowsDomainException()
    {
        var state = WeatherTestData.CreateWeatherState();

        var exception = Assert.Throws<DomainException>(() => state.HasSameConditionsAs(null!));

        Assert.Equal("Domain.Guard.Null", exception.Code);
        Assert.Equal("other", exception.PropertyName);
    }
}
