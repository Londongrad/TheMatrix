using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather;

public sealed class CityWeatherTests
{
    private const string InvalidClimateProfileCode = "SimulationCore.Weather.ClimateProfile.Invalid";
    private const string InvalidTransitionTimingCode = "SimulationCore.Weather.Transition.Timing.Invalid";
    private const string EvaluationTimeBackwardsCode = "SimulationCore.Weather.Evaluation.Time.Backwards";
    private const string OverrideAlreadyActiveCode = "SimulationCore.Weather.Override.AlreadyActive";
    private const string OverrideNotActiveCode = "SimulationCore.Weather.Override.NotActive";

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

    [Fact]
    public void StartOverride_WithValidForcedState_SetsOverride_AndEmitsStartedAndChangedEvents()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var previousState = cityWeather.CurrentState;
        var forcedState = CreateForcedOverrideState();

        cityWeather.ClearDomainEvents();
        cityWeather.StartOverride(
            forcedState: forcedState,
            source: WeatherOverrideSource.Scenario,
            reason: "  scripted storm  ");

        Assert.NotNull(cityWeather.ActiveOverride);
        Assert.Equal(forcedState, cityWeather.ActiveOverride!.ForcedState);
        Assert.Equal(WeatherOverrideSource.Scenario, cityWeather.ActiveOverride.Source);
        Assert.Equal("scripted storm", cityWeather.ActiveOverride.Reason);
        Assert.Equal(forcedState, cityWeather.CurrentState);
        Assert.Equal(WeatherTestData.Midpoint, cityWeather.LastEvaluatedAt);
        Assert.Equal(WeatherTestData.Midpoint, cityWeather.LastTransitionAt);

        Assert.Collection(
            cityWeather.DomainEvents,
            domainEvent =>
            {
                var startedEvent = Assert.IsType<WeatherOverrideStartedDomainEvent>(domainEvent);
                Assert.Equal(WeatherTestData.CityId, startedEvent.CityId);
                Assert.Equal(forcedState, startedEvent.ForcedState);
                Assert.Equal(WeatherOverrideSource.Scenario, startedEvent.Source);
                Assert.Equal(forcedState.StartedAt, startedEvent.StartsAt);
                Assert.Equal(forcedState.ExpectedUntil, startedEvent.EndsAt);
                Assert.Equal("scripted storm", startedEvent.Reason);
            },
            domainEvent =>
            {
                var changedEvent = Assert.IsType<CityWeatherChangedDomainEvent>(domainEvent);
                Assert.Equal(WeatherTestData.CityId, changedEvent.CityId);
                Assert.Equal(previousState, changedEvent.PreviousState);
                Assert.Equal(forcedState, changedEvent.CurrentState);
                Assert.Equal(WeatherTestData.Midpoint, changedEvent.AtSimTime);
            });
    }

    [Fact]
    public void StartOverride_WhenOverrideIsAlreadyActive_ThrowsDomainException()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();

        cityWeather.StartOverride(
            forcedState: CreateForcedOverrideState(),
            source: WeatherOverrideSource.Scenario,
            reason: "first");
        cityWeather.ClearDomainEvents();

        var exception = Assert.Throws<DomainException>(() => cityWeather.StartOverride(
            forcedState: CreateForcedOverrideState(
                temperature: TemperatureC.From(7m),
                expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(3))),
            source: WeatherOverrideSource.Manual,
            reason: "second"));

        Assert.Equal(OverrideAlreadyActiveCode, exception.Code);
        Assert.Equal("ActiveOverride", exception.PropertyName);
        Assert.Empty(cityWeather.DomainEvents);
    }

    [Fact]
    public void StartOverride_WhenForcedStateIsNotActiveAtCurrentTime_ThrowsDomainException()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var forcedState = CreateForcedOverrideState(
            startedAt: WeatherTestData.ExpectedUntil,
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(2)));

        cityWeather.ClearDomainEvents();

        var exception = Assert.Throws<DomainException>(() => cityWeather.StartOverride(
            forcedState: forcedState,
            source: WeatherOverrideSource.Scenario,
            reason: null));

        Assert.Equal(InvalidTransitionTimingCode, exception.Code);
        Assert.Equal("state", exception.PropertyName);
        Assert.Empty(cityWeather.DomainEvents);
    }

    [Fact]
    public void CancelOverride_WithActiveOverride_ClearsOverride_AndEmitsCancelledAndChangedEvents()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var forcedState = CreateForcedOverrideState();
        var fallbackState = WeatherTestData.CreateWeatherState(
            type: WeatherType.Rain,
            precipitationKind: PrecipitationKind.Rain,
            severity: WeatherSeverity.Moderate,
            temperature: TemperatureC.From(13m),
            humidity: HumidityPercent.From(84m),
            windSpeed: WindSpeedKph.From(17m),
            cloudCoverage: CloudCoveragePercent.From(90m),
            pressure: PressureHpa.From(1008m),
            startedAt: WeatherTestData.StartedAt,
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(2)));

        cityWeather.StartOverride(
            forcedState: forcedState,
            source: WeatherOverrideSource.System,
            reason: "override");
        cityWeather.ClearDomainEvents();

        cityWeather.CancelOverride(
            cancelledAt: WeatherTestData.NearEnd,
            fallbackState: fallbackState);

        Assert.Null(cityWeather.ActiveOverride);
        Assert.Equal(fallbackState, cityWeather.CurrentState);
        Assert.Equal(WeatherTestData.NearEnd, cityWeather.LastEvaluatedAt);
        Assert.Equal(WeatherTestData.NearEnd, cityWeather.LastTransitionAt);

        Assert.Collection(
            cityWeather.DomainEvents,
            domainEvent =>
            {
                var cancelledEvent = Assert.IsType<WeatherOverrideCancelledDomainEvent>(domainEvent);
                Assert.Equal(WeatherTestData.CityId, cancelledEvent.CityId);
                Assert.Equal(forcedState, cancelledEvent.ForcedState);
                Assert.Equal(WeatherOverrideSource.System, cancelledEvent.Source);
                Assert.Equal(WeatherTestData.NearEnd, cancelledEvent.CancelledAt);
            },
            domainEvent =>
            {
                var changedEvent = Assert.IsType<CityWeatherChangedDomainEvent>(domainEvent);
                Assert.Equal(WeatherTestData.CityId, changedEvent.CityId);
                Assert.Equal(forcedState, changedEvent.PreviousState);
                Assert.Equal(fallbackState, changedEvent.CurrentState);
                Assert.Equal(WeatherTestData.NearEnd, changedEvent.AtSimTime);
            });
    }

    [Fact]
    public void CancelOverride_WhenNoOverrideIsActive_ThrowsDomainException()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var fallbackState = WeatherTestData.CreateWeatherState(
            startedAt: WeatherTestData.StartedAt,
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(1)));

        cityWeather.ClearDomainEvents();

        var exception = Assert.Throws<DomainException>(() => cityWeather.CancelOverride(
            cancelledAt: WeatherTestData.NearEnd,
            fallbackState: fallbackState));

        Assert.Equal(OverrideNotActiveCode, exception.Code);
        Assert.Equal("ActiveOverride", exception.PropertyName);
        Assert.Empty(cityWeather.DomainEvents);
    }

    [Fact]
    public void ExpireOverrideIfNeeded_WhenOverrideIsStillActive_ReturnsFalse_AndKeepsOverride()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var forcedState = CreateForcedOverrideState(
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(2)));
        var fallbackState = WeatherTestData.CreateWeatherState(
            startedAt: WeatherTestData.StartedAt,
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(4)));

        cityWeather.StartOverride(
            forcedState: forcedState,
            source: WeatherOverrideSource.Scenario,
            reason: "storm");
        cityWeather.ClearDomainEvents();

        var result = cityWeather.ExpireOverrideIfNeeded(
            evaluatedAt: WeatherTestData.NearEnd,
            fallbackState: fallbackState);

        Assert.False(result);
        Assert.NotNull(cityWeather.ActiveOverride);
        Assert.Equal(forcedState, cityWeather.CurrentState);
        Assert.Equal(WeatherTestData.NearEnd, cityWeather.LastEvaluatedAt);
        Assert.Equal(WeatherTestData.Midpoint, cityWeather.LastTransitionAt);
        Assert.Empty(cityWeather.DomainEvents);
    }

    [Fact]
    public void ExpireOverrideIfNeeded_WhenOverrideHasExpired_ReturnsTrue_AndEmitsExpiredAndChangedEvents()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var forcedState = CreateForcedOverrideState(
            expectedUntil: WeatherTestData.ExpectedUntil);
        var fallbackState = WeatherTestData.CreateWeatherState(
            type: WeatherType.Overcast,
            precipitationKind: PrecipitationKind.None,
            severity: WeatherSeverity.Mild,
            temperature: TemperatureC.From(10m),
            humidity: HumidityPercent.From(68m),
            windSpeed: WindSpeedKph.From(11m),
            cloudCoverage: CloudCoveragePercent.From(74m),
            pressure: PressureHpa.From(1010m),
            startedAt: WeatherTestData.ExpectedUntil,
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(3)));

        cityWeather.StartOverride(
            forcedState: forcedState,
            source: WeatherOverrideSource.Manual,
            reason: "manual");
        cityWeather.ClearDomainEvents();

        var result = cityWeather.ExpireOverrideIfNeeded(
            evaluatedAt: WeatherTestData.AfterEnd,
            fallbackState: fallbackState);

        Assert.True(result);
        Assert.Null(cityWeather.ActiveOverride);
        Assert.Equal(fallbackState, cityWeather.CurrentState);
        Assert.Equal(WeatherTestData.AfterEnd, cityWeather.LastEvaluatedAt);
        Assert.Equal(WeatherTestData.AfterEnd, cityWeather.LastTransitionAt);

        Assert.Collection(
            cityWeather.DomainEvents,
            domainEvent =>
            {
                var expiredEvent = Assert.IsType<WeatherOverrideExpiredDomainEvent>(domainEvent);
                Assert.Equal(WeatherTestData.CityId, expiredEvent.CityId);
                Assert.Equal(forcedState, expiredEvent.ForcedState);
                Assert.Equal(WeatherOverrideSource.Manual, expiredEvent.Source);
                Assert.Equal(WeatherTestData.AfterEnd, expiredEvent.ExpiredAt);
            },
            domainEvent =>
            {
                var changedEvent = Assert.IsType<CityWeatherChangedDomainEvent>(domainEvent);
                Assert.Equal(WeatherTestData.CityId, changedEvent.CityId);
                Assert.Equal(forcedState, changedEvent.PreviousState);
                Assert.Equal(fallbackState, changedEvent.CurrentState);
                Assert.Equal(WeatherTestData.AfterEnd, changedEvent.AtSimTime);
            });
    }

    [Fact]
    public void AdvanceTo_WhenOverrideIsStillActive_IgnoresNextState_AndOnlyUpdatesEvaluationTime()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var forcedState = CreateForcedOverrideState(
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(2)));
        var ignoredNextState = WeatherTestData.CreateWeatherState(
            type: WeatherType.Snow,
            precipitationKind: PrecipitationKind.Snow,
            severity: WeatherSeverity.Moderate,
            temperature: TemperatureC.From(-4m),
            humidity: HumidityPercent.From(76m),
            windSpeed: WindSpeedKph.From(15m),
            cloudCoverage: CloudCoveragePercent.From(88m),
            pressure: PressureHpa.From(1004m),
            startedAt: WeatherTestData.StartedAt,
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(1)));

        cityWeather.StartOverride(
            forcedState: forcedState,
            source: WeatherOverrideSource.Scenario,
            reason: "storm");
        cityWeather.ClearDomainEvents();

        cityWeather.AdvanceTo(
            evaluatedAt: WeatherTestData.NearEnd,
            nextState: ignoredNextState);

        Assert.NotNull(cityWeather.ActiveOverride);
        Assert.Equal(forcedState, cityWeather.CurrentState);
        Assert.Equal(WeatherTestData.NearEnd, cityWeather.LastEvaluatedAt);
        Assert.Equal(WeatherTestData.Midpoint, cityWeather.LastTransitionAt);
        Assert.Empty(cityWeather.DomainEvents);
    }

    [Fact]
    public void AdvanceTo_WhenOverrideExpires_EmitsExpiredAndChangedEventsUsingFallbackState()
    {
        var cityWeather = WeatherTestData.CreateCityWeather();
        var forcedState = CreateForcedOverrideState(
            expectedUntil: WeatherTestData.ExpectedUntil);
        var fallbackState = WeatherTestData.CreateWeatherState(
            type: WeatherType.Clear,
            precipitationKind: PrecipitationKind.None,
            severity: WeatherSeverity.Calm,
            temperature: TemperatureC.From(9m),
            humidity: HumidityPercent.From(52m),
            windSpeed: WindSpeedKph.From(8m),
            cloudCoverage: CloudCoveragePercent.From(5m),
            pressure: PressureHpa.From(1015m),
            startedAt: WeatherTestData.ExpectedUntil,
            expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(2)));

        cityWeather.StartOverride(
            forcedState: forcedState,
            source: WeatherOverrideSource.Debug,
            reason: "debug");
        cityWeather.ClearDomainEvents();

        cityWeather.AdvanceTo(
            evaluatedAt: WeatherTestData.AfterEnd,
            nextState: fallbackState);

        Assert.Null(cityWeather.ActiveOverride);
        Assert.Equal(fallbackState, cityWeather.CurrentState);
        Assert.Equal(WeatherTestData.AfterEnd, cityWeather.LastEvaluatedAt);
        Assert.Equal(WeatherTestData.AfterEnd, cityWeather.LastTransitionAt);

        Assert.Collection(
            cityWeather.DomainEvents,
            domainEvent =>
            {
                var expiredEvent = Assert.IsType<WeatherOverrideExpiredDomainEvent>(domainEvent);
                Assert.Equal(WeatherTestData.CityId, expiredEvent.CityId);
                Assert.Equal(forcedState, expiredEvent.ForcedState);
                Assert.Equal(WeatherOverrideSource.Debug, expiredEvent.Source);
                Assert.Equal(WeatherTestData.AfterEnd, expiredEvent.ExpiredAt);
            },
            domainEvent =>
            {
                var changedEvent = Assert.IsType<CityWeatherChangedDomainEvent>(domainEvent);
                Assert.Equal(WeatherTestData.CityId, changedEvent.CityId);
                Assert.Equal(forcedState, changedEvent.PreviousState);
                Assert.Equal(fallbackState, changedEvent.CurrentState);
                Assert.Equal(WeatherTestData.AfterEnd, changedEvent.AtSimTime);
            });
    }

    private static WeatherState CreateForcedOverrideState(
        TemperatureC? temperature = null,
        SimTime? startedAt = null,
        SimTime? expectedUntil = null)
    {
        return WeatherTestData.CreateWeatherState(
            type: WeatherType.Storm,
            precipitationKind: PrecipitationKind.Hail,
            severity: WeatherSeverity.Severe,
            temperature: temperature ?? TemperatureC.From(6m),
            humidity: HumidityPercent.From(93m),
            windSpeed: WindSpeedKph.From(34m),
            cloudCoverage: CloudCoveragePercent.From(97m),
            pressure: PressureHpa.From(998m),
            startedAt: startedAt ?? WeatherTestData.StartedAt,
            expectedUntil: expectedUntil ?? WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(2)));
    }
}
