using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather;

public sealed class CityWeatherTests
{
    private const string InvalidClimateProfileCode = "SimulationCore.Weather.ClimateProfile.Invalid";
    private const string InvalidTransitionTimingCode = "SimulationCore.Weather.Transition.Timing.Invalid";
    private const string EvaluationTimeBackwardsCode = "SimulationCore.Weather.Evaluation.Time.Backwards";

    [Fact]
    public void Create_WithValidValues_SetsState_AndEmitsCreatedEvent()
    {
        var climateProfile = WeatherTestData.CreateClimateProfile();
        var currentState = WeatherTestData.CreateWeatherState();

        var cityWeather = CityWeather.Create(
            cityId: WeatherTestData.CityId,
            climateProfile: climateProfile,
            currentState: currentState,
            createdAt: WeatherTestData.Midpoint);

        Assert.Equal(WeatherTestData.CityId, cityWeather.CityId);
        Assert.Equal(climateProfile, cityWeather.ClimateProfile);
        Assert.Equal(currentState, cityWeather.CurrentState);
        Assert.Null(cityWeather.ActiveOverride);
        Assert.Equal(WeatherTestData.Midpoint, cityWeather.LastEvaluatedAt);
        Assert.Equal(WeatherTestData.Midpoint, cityWeather.LastTransitionAt);

        var createdEvent = Assert.IsType<CityWeatherCreatedDomainEvent>(Assert.Single(cityWeather.DomainEvents));

        Assert.Equal(WeatherTestData.CityId, createdEvent.CityId);
        Assert.Equal(currentState, createdEvent.InitialState);
        Assert.Equal(climateProfile, createdEvent.ClimateProfile);
        Assert.Equal(WeatherTestData.Midpoint, createdEvent.AtSimTime);
    }

    [Fact]
    public void Create_WithMissingClimateProfile_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityWeather.Create(
            cityId: WeatherTestData.CityId,
            climateProfile: null!,
            currentState: WeatherTestData.CreateWeatherState(),
            createdAt: WeatherTestData.Midpoint));

        Assert.Equal(InvalidClimateProfileCode, exception.Code);
        Assert.Equal("climateProfile", exception.PropertyName);
    }

    [Fact]
    public void Create_WhenCurrentStateIsNotActiveAtCreatedAt_ThrowsDomainException()
    {
        var inactiveState = WeatherTestData.CreateWeatherState(
            startedAt: WeatherTestData.StartedAt,
            expectedUntil: WeatherTestData.ExpectedUntil);

        var exception = Assert.Throws<DomainException>(() => CityWeather.Create(
            cityId: WeatherTestData.CityId,
            climateProfile: WeatherTestData.CreateClimateProfile(),
            currentState: inactiveState,
            createdAt: WeatherTestData.AfterEnd));

        Assert.Equal(InvalidTransitionTimingCode, exception.Code);
        Assert.Equal("state", exception.PropertyName);
    }

    [Fact]
    public void AdvanceTo_WhenConditionsChange_UpdatesState_AndEmitsChangedEvent()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var nextState = WeatherTestData.CreateWeatherState(
            type: WeatherType.Rain,
            precipitationKind: PrecipitationKind.Rain,
            severity: WeatherSeverity.Moderate,
            temperature: TemperatureC.From(14m),
            humidity: HumidityPercent.From(81m),
            windSpeed: WindSpeedKph.From(18m),
            cloudCoverage: CloudCoveragePercent.From(92m),
            pressure: PressureHpa.From(1006m),
            startedAt: WeatherTestData.Midpoint,
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(2)));

        cityWeather.ClearDomainEvents();
        cityWeather.AdvanceTo(
            evaluatedAt: WeatherTestData.NearEnd,
            nextState: nextState);

        Assert.Equal(nextState, cityWeather.CurrentState);
        Assert.Equal(WeatherTestData.NearEnd, cityWeather.LastEvaluatedAt);
        Assert.Equal(WeatherTestData.NearEnd, cityWeather.LastTransitionAt);

        var changedEvent = Assert.IsType<CityWeatherChangedDomainEvent>(Assert.Single(cityWeather.DomainEvents));

        Assert.Equal(WeatherTestData.CityId, changedEvent.CityId);
        Assert.Equal(WeatherTestData.CreateWeatherState(), changedEvent.PreviousState);
        Assert.Equal(nextState, changedEvent.CurrentState);
        Assert.Equal(WeatherTestData.NearEnd, changedEvent.AtSimTime);
    }

    [Fact]
    public void AdvanceTo_WhenConditionsMatchButWindowDiffers_UpdatesCurrentState_WithoutEvent()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var nextState = WeatherTestData.CreateWeatherState(
            startedAt: WeatherTestData.Midpoint,
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(1)));

        cityWeather.ClearDomainEvents();
        cityWeather.AdvanceTo(
            evaluatedAt: WeatherTestData.NearEnd,
            nextState: nextState);

        Assert.Equal(nextState, cityWeather.CurrentState);
        Assert.Equal(WeatherTestData.NearEnd, cityWeather.LastEvaluatedAt);
        Assert.Equal(WeatherTestData.Midpoint, cityWeather.LastTransitionAt);
        Assert.Empty(cityWeather.DomainEvents);
    }

    [Fact]
    public void AdvanceTo_WhenEvaluationGoesBackwards_ThrowsDomainException()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var nextState = WeatherTestData.CreateWeatherState(
            startedAt: WeatherTestData.StartedAt,
            expectedUntil: WeatherTestData.ExpectedUntil);

        cityWeather.ClearDomainEvents();

        var exception = Assert.Throws<DomainException>(() => cityWeather.AdvanceTo(
            evaluatedAt: WeatherTestData.StartedAt,
            nextState: nextState));

        Assert.Equal(EvaluationTimeBackwardsCode, exception.Code);
        Assert.Equal("evaluatedAt", exception.PropertyName);
        Assert.Empty(cityWeather.DomainEvents);
    }

    [Fact]
    public void ChangeClimateProfile_WhenProfileChanges_UpdatesProfile_AndEmitsEvent()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var newProfile = WeatherTestData.CreateAlternativeClimateProfile();

        cityWeather.ClearDomainEvents();
        cityWeather.ChangeClimateProfile(
            newClimateProfile: newProfile,
            changedAt: WeatherTestData.NearEnd);

        Assert.Equal(newProfile, cityWeather.ClimateProfile);
        Assert.Equal(WeatherTestData.NearEnd, cityWeather.LastEvaluatedAt);

        var changedEvent = Assert.IsType<ClimateProfileChangedDomainEvent>(Assert.Single(cityWeather.DomainEvents));

        Assert.Equal(WeatherTestData.CityId, changedEvent.CityId);
        Assert.Equal(WeatherTestData.CreateClimateProfile(), changedEvent.PreviousProfile);
        Assert.Equal(newProfile, changedEvent.CurrentProfile);
        Assert.Equal(WeatherTestData.NearEnd, changedEvent.AtSimTime);
    }

    [Fact]
    public void ChangeClimateProfile_WithSameProfile_IsNoOpExceptLastEvaluatedAt()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var profile = WeatherTestData.CreateClimateProfile();

        cityWeather.ClearDomainEvents();
        cityWeather.ChangeClimateProfile(
            newClimateProfile: profile,
            changedAt: WeatherTestData.NearEnd);

        Assert.Equal(profile, cityWeather.ClimateProfile);
        Assert.Equal(WeatherTestData.NearEnd, cityWeather.LastEvaluatedAt);
        Assert.Empty(cityWeather.DomainEvents);
    }
}
