using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather
{
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
            WeatherClimateProfile climateProfile = WeatherTestData.CreateClimateProfile();
            WeatherState currentState = WeatherTestData.CreateWeatherState();

            var cityWeather = CityWeather.Create(
                cityId: WeatherTestData.CityId,
                climateProfile: climateProfile,
                currentState: currentState,
                createdAt: WeatherTestData.Midpoint);

            Assert.Equal(
                expected: WeatherTestData.CityId,
                actual: cityWeather.CityId);
            Assert.Equal(
                expected: climateProfile,
                actual: cityWeather.ClimateProfile);
            Assert.Equal(
                expected: currentState,
                actual: cityWeather.CurrentState);
            Assert.Null(cityWeather.ActiveOverride);
            Assert.Equal(
                expected: WeatherTestData.Midpoint,
                actual: cityWeather.LastEvaluatedAt);
            Assert.Equal(
                expected: WeatherTestData.Midpoint,
                actual: cityWeather.LastTransitionAt);

            CityWeatherCreatedDomainEvent createdEvent =
                Assert.IsType<CityWeatherCreatedDomainEvent>(Assert.Single(cityWeather.DomainEvents));

            Assert.Equal(
                expected: WeatherTestData.CityId,
                actual: createdEvent.CityId);
            Assert.Equal(
                expected: currentState,
                actual: createdEvent.InitialState);
            Assert.Equal(
                expected: climateProfile,
                actual: createdEvent.ClimateProfile);
            Assert.Equal(
                expected: WeatherTestData.Midpoint,
                actual: createdEvent.AtSimTime);
        }

        [Fact]
        public void Create_WithMissingClimateProfile_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityWeather.Create(
                cityId: WeatherTestData.CityId,
                climateProfile: null!,
                currentState: WeatherTestData.CreateWeatherState(),
                createdAt: WeatherTestData.Midpoint));

            Assert.Equal(
                expected: InvalidClimateProfileCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "climateProfile",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WhenCurrentStateIsNotActiveAtCreatedAt_ThrowsDomainException()
        {
            WeatherState inactiveState = WeatherTestData.CreateWeatherState(
                startedAt: WeatherTestData.StartedAt,
                expectedUntil: WeatherTestData.ExpectedUntil);

            DomainException exception = Assert.Throws<DomainException>(() => CityWeather.Create(
                cityId: WeatherTestData.CityId,
                climateProfile: WeatherTestData.CreateClimateProfile(),
                currentState: inactiveState,
                createdAt: WeatherTestData.AfterEnd));

            Assert.Equal(
                expected: InvalidTransitionTimingCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "state",
                actual: exception.PropertyName);
        }

        [Fact]
        public void AdvanceTo_WhenConditionsChange_UpdatesState_AndEmitsChangedEvent()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState nextState = WeatherTestData.CreateWeatherState(
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

            Assert.Equal(
                expected: nextState,
                actual: cityWeather.CurrentState);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: cityWeather.LastEvaluatedAt);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: cityWeather.LastTransitionAt);

            CityWeatherChangedDomainEvent changedEvent =
                Assert.IsType<CityWeatherChangedDomainEvent>(Assert.Single(cityWeather.DomainEvents));

            Assert.Equal(
                expected: WeatherTestData.CityId,
                actual: changedEvent.CityId);
            Assert.Equal(
                expected: WeatherTestData.CreateWeatherState(),
                actual: changedEvent.PreviousState);
            Assert.Equal(
                expected: nextState,
                actual: changedEvent.CurrentState);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: changedEvent.AtSimTime);
        }

        [Fact]
        public void AdvanceTo_WhenConditionsMatchButWindowDiffers_UpdatesCurrentState_WithoutEvent()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState nextState = WeatherTestData.CreateWeatherState(
                startedAt: WeatherTestData.Midpoint,
                expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(1)));

            cityWeather.ClearDomainEvents();
            cityWeather.AdvanceTo(
                evaluatedAt: WeatherTestData.NearEnd,
                nextState: nextState);

            Assert.Equal(
                expected: nextState,
                actual: cityWeather.CurrentState);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: cityWeather.LastEvaluatedAt);
            Assert.Equal(
                expected: WeatherTestData.Midpoint,
                actual: cityWeather.LastTransitionAt);
            Assert.Empty(cityWeather.DomainEvents);
        }

        [Fact]
        public void AdvanceTo_WhenEvaluationGoesBackwards_ThrowsDomainException()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState nextState = WeatherTestData.CreateWeatherState(
                startedAt: WeatherTestData.StartedAt,
                expectedUntil: WeatherTestData.ExpectedUntil);

            cityWeather.ClearDomainEvents();

            DomainException exception = Assert.Throws<DomainException>(() => cityWeather.AdvanceTo(
                evaluatedAt: WeatherTestData.StartedAt,
                nextState: nextState));

            Assert.Equal(
                expected: EvaluationTimeBackwardsCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "evaluatedAt",
                actual: exception.PropertyName);
            Assert.Empty(cityWeather.DomainEvents);
        }

        [Fact]
        public void ChangeClimateProfile_WhenProfileChanges_UpdatesProfile_AndEmitsEvent()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherClimateProfile newProfile = WeatherTestData.CreateAlternativeClimateProfile();

            cityWeather.ClearDomainEvents();
            cityWeather.ChangeClimateProfile(
                newClimateProfile: newProfile,
                changedAt: WeatherTestData.NearEnd);

            Assert.Equal(
                expected: newProfile,
                actual: cityWeather.ClimateProfile);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: cityWeather.LastEvaluatedAt);

            ClimateProfileChangedDomainEvent changedEvent =
                Assert.IsType<ClimateProfileChangedDomainEvent>(Assert.Single(cityWeather.DomainEvents));

            Assert.Equal(
                expected: WeatherTestData.CityId,
                actual: changedEvent.CityId);
            Assert.Equal(
                expected: WeatherTestData.CreateClimateProfile(),
                actual: changedEvent.PreviousProfile);
            Assert.Equal(
                expected: newProfile,
                actual: changedEvent.CurrentProfile);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: changedEvent.AtSimTime);
        }

        [Fact]
        public void ChangeClimateProfile_WithSameProfile_IsNoOpExceptLastEvaluatedAt()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherClimateProfile profile = WeatherTestData.CreateClimateProfile();

            cityWeather.ClearDomainEvents();
            cityWeather.ChangeClimateProfile(
                newClimateProfile: profile,
                changedAt: WeatherTestData.NearEnd);

            Assert.Equal(
                expected: profile,
                actual: cityWeather.ClimateProfile);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: cityWeather.LastEvaluatedAt);
            Assert.Empty(cityWeather.DomainEvents);
        }

        [Fact]
        public void StartOverride_WithValidForcedState_SetsOverride_AndEmitsStartedAndChangedEvents()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState previousState = cityWeather.CurrentState;
            WeatherState forcedState = CreateForcedOverrideState();

            cityWeather.ClearDomainEvents();
            cityWeather.StartOverride(
                forcedState: forcedState,
                source: WeatherOverrideSource.Scenario,
                reason: "  scripted storm  ");

            Assert.NotNull(cityWeather.ActiveOverride);
            Assert.Equal(
                expected: forcedState,
                actual: cityWeather.ActiveOverride!.ForcedState);
            Assert.Equal(
                expected: WeatherOverrideSource.Scenario,
                actual: cityWeather.ActiveOverride.Source);
            Assert.Equal(
                expected: "scripted storm",
                actual: cityWeather.ActiveOverride.Reason);
            Assert.Equal(
                expected: forcedState,
                actual: cityWeather.CurrentState);
            Assert.Equal(
                expected: WeatherTestData.Midpoint,
                actual: cityWeather.LastEvaluatedAt);
            Assert.Equal(
                expected: WeatherTestData.Midpoint,
                actual: cityWeather.LastTransitionAt);

            Assert.Collection(
                collection: cityWeather.DomainEvents,
                domainEvent =>
                {
                    WeatherOverrideStartedDomainEvent startedEvent =
                        Assert.IsType<WeatherOverrideStartedDomainEvent>(domainEvent);
                    Assert.Equal(
                        expected: WeatherTestData.CityId,
                        actual: startedEvent.CityId);
                    Assert.Equal(
                        expected: forcedState,
                        actual: startedEvent.ForcedState);
                    Assert.Equal(
                        expected: WeatherOverrideSource.Scenario,
                        actual: startedEvent.Source);
                    Assert.Equal(
                        expected: forcedState.StartedAt,
                        actual: startedEvent.StartsAt);
                    Assert.Equal(
                        expected: forcedState.ExpectedUntil,
                        actual: startedEvent.EndsAt);
                    Assert.Equal(
                        expected: "scripted storm",
                        actual: startedEvent.Reason);
                },
                domainEvent =>
                {
                    CityWeatherChangedDomainEvent changedEvent =
                        Assert.IsType<CityWeatherChangedDomainEvent>(domainEvent);
                    Assert.Equal(
                        expected: WeatherTestData.CityId,
                        actual: changedEvent.CityId);
                    Assert.Equal(
                        expected: previousState,
                        actual: changedEvent.PreviousState);
                    Assert.Equal(
                        expected: forcedState,
                        actual: changedEvent.CurrentState);
                    Assert.Equal(
                        expected: WeatherTestData.Midpoint,
                        actual: changedEvent.AtSimTime);
                });
        }

        [Fact]
        public void StartOverride_WhenOverrideIsAlreadyActive_ThrowsDomainException()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();

            cityWeather.StartOverride(
                forcedState: CreateForcedOverrideState(),
                source: WeatherOverrideSource.Scenario,
                reason: "first");
            cityWeather.ClearDomainEvents();

            DomainException exception = Assert.Throws<DomainException>(() => cityWeather.StartOverride(
                forcedState: CreateForcedOverrideState(
                    temperature: TemperatureC.From(7m),
                    expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(3))),
                source: WeatherOverrideSource.Manual,
                reason: "second"));

            Assert.Equal(
                expected: OverrideAlreadyActiveCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "ActiveOverride",
                actual: exception.PropertyName);
            Assert.Empty(cityWeather.DomainEvents);
        }

        [Fact]
        public void StartOverride_WhenForcedStateIsNotActiveAtCurrentTime_ThrowsDomainException()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState forcedState = CreateForcedOverrideState(
                startedAt: WeatherTestData.ExpectedUntil,
                expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(2)));

            cityWeather.ClearDomainEvents();

            DomainException exception = Assert.Throws<DomainException>(() => cityWeather.StartOverride(
                forcedState: forcedState,
                source: WeatherOverrideSource.Scenario,
                reason: null));

            Assert.Equal(
                expected: InvalidTransitionTimingCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "state",
                actual: exception.PropertyName);
            Assert.Empty(cityWeather.DomainEvents);
        }

        [Fact]
        public void CancelOverride_WithActiveOverride_ClearsOverride_AndEmitsCancelledAndChangedEvents()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState forcedState = CreateForcedOverrideState();
            WeatherState fallbackState = WeatherTestData.CreateWeatherState(
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
            Assert.Equal(
                expected: fallbackState,
                actual: cityWeather.CurrentState);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: cityWeather.LastEvaluatedAt);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: cityWeather.LastTransitionAt);

            Assert.Collection(
                collection: cityWeather.DomainEvents,
                domainEvent =>
                {
                    WeatherOverrideCancelledDomainEvent cancelledEvent =
                        Assert.IsType<WeatherOverrideCancelledDomainEvent>(domainEvent);
                    Assert.Equal(
                        expected: WeatherTestData.CityId,
                        actual: cancelledEvent.CityId);
                    Assert.Equal(
                        expected: forcedState,
                        actual: cancelledEvent.ForcedState);
                    Assert.Equal(
                        expected: WeatherOverrideSource.System,
                        actual: cancelledEvent.Source);
                    Assert.Equal(
                        expected: WeatherTestData.NearEnd,
                        actual: cancelledEvent.CancelledAt);
                },
                domainEvent =>
                {
                    CityWeatherChangedDomainEvent changedEvent =
                        Assert.IsType<CityWeatherChangedDomainEvent>(domainEvent);
                    Assert.Equal(
                        expected: WeatherTestData.CityId,
                        actual: changedEvent.CityId);
                    Assert.Equal(
                        expected: forcedState,
                        actual: changedEvent.PreviousState);
                    Assert.Equal(
                        expected: fallbackState,
                        actual: changedEvent.CurrentState);
                    Assert.Equal(
                        expected: WeatherTestData.NearEnd,
                        actual: changedEvent.AtSimTime);
                });
        }

        [Fact]
        public void CancelOverride_WhenNoOverrideIsActive_ThrowsDomainException()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState fallbackState = WeatherTestData.CreateWeatherState(
                startedAt: WeatherTestData.StartedAt,
                expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(1)));

            cityWeather.ClearDomainEvents();

            DomainException exception = Assert.Throws<DomainException>(() => cityWeather.CancelOverride(
                cancelledAt: WeatherTestData.NearEnd,
                fallbackState: fallbackState));

            Assert.Equal(
                expected: OverrideNotActiveCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "ActiveOverride",
                actual: exception.PropertyName);
            Assert.Empty(cityWeather.DomainEvents);
        }

        [Fact]
        public void ExpireOverrideIfNeeded_WhenOverrideIsStillActive_ReturnsFalse_AndKeepsOverride()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState forcedState = CreateForcedOverrideState(
                expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(2)));
            WeatherState fallbackState = WeatherTestData.CreateWeatherState(
                startedAt: WeatherTestData.StartedAt,
                expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(4)));

            cityWeather.StartOverride(
                forcedState: forcedState,
                source: WeatherOverrideSource.Scenario,
                reason: "storm");
            cityWeather.ClearDomainEvents();

            bool result = cityWeather.ExpireOverrideIfNeeded(
                evaluatedAt: WeatherTestData.NearEnd,
                fallbackState: fallbackState);

            Assert.False(result);
            Assert.NotNull(cityWeather.ActiveOverride);
            Assert.Equal(
                expected: forcedState,
                actual: cityWeather.CurrentState);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: cityWeather.LastEvaluatedAt);
            Assert.Equal(
                expected: WeatherTestData.Midpoint,
                actual: cityWeather.LastTransitionAt);
            Assert.Empty(cityWeather.DomainEvents);
        }

        [Fact]
        public void ExpireOverrideIfNeeded_WhenOverrideHasExpired_ReturnsTrue_AndEmitsExpiredAndChangedEvents()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState forcedState = CreateForcedOverrideState(expectedUntil: WeatherTestData.ExpectedUntil);
            WeatherState fallbackState = WeatherTestData.CreateWeatherState(
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

            bool result = cityWeather.ExpireOverrideIfNeeded(
                evaluatedAt: WeatherTestData.AfterEnd,
                fallbackState: fallbackState);

            Assert.True(result);
            Assert.Null(cityWeather.ActiveOverride);
            Assert.Equal(
                expected: fallbackState,
                actual: cityWeather.CurrentState);
            Assert.Equal(
                expected: WeatherTestData.AfterEnd,
                actual: cityWeather.LastEvaluatedAt);
            Assert.Equal(
                expected: WeatherTestData.AfterEnd,
                actual: cityWeather.LastTransitionAt);

            Assert.Collection(
                collection: cityWeather.DomainEvents,
                domainEvent =>
                {
                    WeatherOverrideExpiredDomainEvent expiredEvent =
                        Assert.IsType<WeatherOverrideExpiredDomainEvent>(domainEvent);
                    Assert.Equal(
                        expected: WeatherTestData.CityId,
                        actual: expiredEvent.CityId);
                    Assert.Equal(
                        expected: forcedState,
                        actual: expiredEvent.ForcedState);
                    Assert.Equal(
                        expected: WeatherOverrideSource.Manual,
                        actual: expiredEvent.Source);
                    Assert.Equal(
                        expected: WeatherTestData.AfterEnd,
                        actual: expiredEvent.ExpiredAt);
                },
                domainEvent =>
                {
                    CityWeatherChangedDomainEvent changedEvent =
                        Assert.IsType<CityWeatherChangedDomainEvent>(domainEvent);
                    Assert.Equal(
                        expected: WeatherTestData.CityId,
                        actual: changedEvent.CityId);
                    Assert.Equal(
                        expected: forcedState,
                        actual: changedEvent.PreviousState);
                    Assert.Equal(
                        expected: fallbackState,
                        actual: changedEvent.CurrentState);
                    Assert.Equal(
                        expected: WeatherTestData.AfterEnd,
                        actual: changedEvent.AtSimTime);
                });
        }

        [Fact]
        public void AdvanceTo_WhenOverrideIsStillActive_IgnoresNextState_AndOnlyUpdatesEvaluationTime()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState forcedState = CreateForcedOverrideState(
                expectedUntil: WeatherTestData.ExpectedUntil.Add(TimeSpan.FromHours(2)));
            WeatherState ignoredNextState = WeatherTestData.CreateWeatherState(
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
            Assert.Equal(
                expected: forcedState,
                actual: cityWeather.CurrentState);
            Assert.Equal(
                expected: WeatherTestData.NearEnd,
                actual: cityWeather.LastEvaluatedAt);
            Assert.Equal(
                expected: WeatherTestData.Midpoint,
                actual: cityWeather.LastTransitionAt);
            Assert.Empty(cityWeather.DomainEvents);
        }

        [Fact]
        public void AdvanceTo_WhenOverrideExpires_EmitsExpiredAndChangedEventsUsingFallbackState()
        {
            CityWeather cityWeather = WeatherTestData.CreateCityWeather();
            WeatherState forcedState = CreateForcedOverrideState(expectedUntil: WeatherTestData.ExpectedUntil);
            WeatherState fallbackState = WeatherTestData.CreateWeatherState(
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
            Assert.Equal(
                expected: fallbackState,
                actual: cityWeather.CurrentState);
            Assert.Equal(
                expected: WeatherTestData.AfterEnd,
                actual: cityWeather.LastEvaluatedAt);
            Assert.Equal(
                expected: WeatherTestData.AfterEnd,
                actual: cityWeather.LastTransitionAt);

            Assert.Collection(
                collection: cityWeather.DomainEvents,
                domainEvent =>
                {
                    WeatherOverrideExpiredDomainEvent expiredEvent =
                        Assert.IsType<WeatherOverrideExpiredDomainEvent>(domainEvent);
                    Assert.Equal(
                        expected: WeatherTestData.CityId,
                        actual: expiredEvent.CityId);
                    Assert.Equal(
                        expected: forcedState,
                        actual: expiredEvent.ForcedState);
                    Assert.Equal(
                        expected: WeatherOverrideSource.Debug,
                        actual: expiredEvent.Source);
                    Assert.Equal(
                        expected: WeatherTestData.AfterEnd,
                        actual: expiredEvent.ExpiredAt);
                },
                domainEvent =>
                {
                    CityWeatherChangedDomainEvent changedEvent =
                        Assert.IsType<CityWeatherChangedDomainEvent>(domainEvent);
                    Assert.Equal(
                        expected: WeatherTestData.CityId,
                        actual: changedEvent.CityId);
                    Assert.Equal(
                        expected: forcedState,
                        actual: changedEvent.PreviousState);
                    Assert.Equal(
                        expected: fallbackState,
                        actual: changedEvent.CurrentState);
                    Assert.Equal(
                        expected: WeatherTestData.AfterEnd,
                        actual: changedEvent.AtSimTime);
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
}
