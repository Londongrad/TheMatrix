using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather;

public sealed class WeatherOverrideTests
{
    [Fact]
    public void Create_WithValidValues_CreatesOverride_AndTrimsReason()
    {
        var forcedState = WeatherTestData.CreateWeatherState(
            type: WeatherType.Storm,
            precipitationKind: PrecipitationKind.Hail,
            severity: WeatherSeverity.Severe);

        var weatherOverride = WeatherOverride.Create(
            forcedState: forcedState,
            source: WeatherOverrideSource.Manual,
            reason: "  operator request  ");

        Assert.NotEqual(Guid.Empty, weatherOverride.Id);
        Assert.Equal(forcedState, weatherOverride.ForcedState);
        Assert.Equal(WeatherOverrideSource.Manual, weatherOverride.Source);
        Assert.Equal("operator request", weatherOverride.Reason);
        Assert.Equal(forcedState.StartedAt, weatherOverride.StartsAt);
        Assert.Equal(forcedState.ExpectedUntil, weatherOverride.EndsAt);
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
        var exception = Assert.Throws<DomainException>(() => WeatherOverride.Create(
            forcedState: null!,
            source: WeatherOverrideSource.Manual));

        Assert.Equal("Domain.Guard.Null", exception.Code);
        Assert.Equal("forcedState", exception.PropertyName);
    }

    [Fact]
    public void Create_WithInvalidSource_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => WeatherOverride.Create(
            forcedState: WeatherTestData.CreateWeatherState(),
            source: (WeatherOverrideSource)999));

        Assert.Equal("Domain.Guard.InvalidEnum", exception.Code);
        Assert.Equal("Source", exception.PropertyName);
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
