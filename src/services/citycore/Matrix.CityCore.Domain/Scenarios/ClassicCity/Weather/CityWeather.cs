using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.CityCore.Domain.Errors;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather
{
    /// <summary>
    ///     Aggregate root that owns the current weather state for a single city.
    /// </summary>
    public sealed class CityWeather : AggregateRoot<CityId>
    {
        private CityWeather(
            CityId cityId,
            WeatherClimateProfile climateProfile,
            WeatherState currentState,
            WeatherOverride? activeOverride,
            SimTime lastEvaluatedAt,
            SimTime lastTransitionAt)
            : base(cityId)
        {
            ClimateProfile = climateProfile;
            CurrentState = currentState;
            ActiveOverride = activeOverride;
            LastEvaluatedAt = lastEvaluatedAt;
            LastTransitionAt = lastTransitionAt;
        }

        private CityWeather()
            : base(default(CityId))
        {
            ClimateProfile = null!;
            CurrentState = null!;
        }

        public CityId CityId => Id;
        public WeatherClimateProfile ClimateProfile { get; private set; }
        public WeatherState CurrentState { get; private set; }
        public WeatherOverride? ActiveOverride { get; private set; }
        public SimTime LastEvaluatedAt { get; private set; }
        public SimTime LastTransitionAt { get; private set; }

        public static CityWeather Create(
            CityId cityId,
            WeatherClimateProfile climateProfile,
            WeatherState currentState,
            SimTime createdAt)
        {
            GuardHelper.AgainstEmptyGuid(
                id: cityId.Value,
                propertyName: nameof(cityId));

            if (climateProfile is null)
                throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Climate profile is required.",
                    propertyName: nameof(climateProfile));

            GuardHelper.AgainstNull(
                value: currentState,
                propertyName: nameof(currentState));

            EnsureStateIsApplicable(
                state: currentState,
                evaluatedAt: createdAt);

            var cityWeather = new CityWeather(
                cityId: cityId,
                climateProfile: climateProfile,
                currentState: currentState,
                activeOverride: null,
                lastEvaluatedAt: createdAt,
                lastTransitionAt: createdAt);

            cityWeather.AddDomainEvent(
                new CityWeatherCreatedDomainEvent(
                    CityId: cityId,
                    InitialState: currentState,
                    ClimateProfile: climateProfile,
                    AtSimTime: createdAt));

            return cityWeather;
        }

        public void AdvanceTo(
            SimTime evaluatedAt,
            WeatherState nextState)
        {
            GuardHelper.AgainstNull(
                value: nextState,
                propertyName: nameof(nextState));

            EnsureEvaluationTimeNotGoingBackwards(evaluatedAt);

            if (ActiveOverride is not null)
            {
                if (!ActiveOverride.HasExpiredBy(evaluatedAt))
                {
                    LastEvaluatedAt = evaluatedAt;
                    return;
                }

                ExpireOverrideInternal(
                    evaluatedAt: evaluatedAt,
                    fallbackState: nextState);
                return;
            }

            ApplyStateAndRaiseChange(
                nextState: nextState,
                evaluatedAt: evaluatedAt);
        }

        public void StartOverride(
            WeatherState forcedState,
            WeatherOverrideSource source,
            string? reason = null)
        {
            GuardHelper.AgainstNull(
                value: forcedState,
                propertyName: nameof(forcedState));
            GuardHelper.AgainstInvalidEnum(
                value: source,
                propertyName: nameof(source));

            if (ActiveOverride is not null)
                throw DomainErrorsFactory.OverrideAlreadyActive(nameof(ActiveOverride));

            EnsureStateIsApplicable(
                state: forcedState,
                evaluatedAt: LastEvaluatedAt);

            var weatherOverride = WeatherOverride.Create(
                forcedState: forcedState,
                source: source,
                reason: reason);

            ActiveOverride = weatherOverride;

            AddDomainEvent(
                new WeatherOverrideStartedDomainEvent(
                    CityId: Id,
                    ForcedState: weatherOverride.ForcedState,
                    Source: weatherOverride.Source,
                    StartsAt: weatherOverride.StartsAt,
                    EndsAt: weatherOverride.EndsAt,
                    Reason: weatherOverride.Reason));

            ApplyStateAndRaiseChange(
                nextState: weatherOverride.ForcedState,
                evaluatedAt: LastEvaluatedAt);
        }

        public void CancelOverride(
            SimTime cancelledAt,
            WeatherState fallbackState)
        {
            GuardHelper.AgainstNull(
                value: fallbackState,
                propertyName: nameof(fallbackState));

            EnsureEvaluationTimeNotGoingBackwards(cancelledAt);

            WeatherOverride activeOverride = ActiveOverride ??
                                             throw DomainErrorsFactory.NoActiveOverrideToCancel(nameof(ActiveOverride));

            ActiveOverride = null;

            AddDomainEvent(
                new WeatherOverrideCancelledDomainEvent(
                    CityId: Id,
                    ForcedState: activeOverride.ForcedState,
                    Source: activeOverride.Source,
                    CancelledAt: cancelledAt));

            ApplyStateAndRaiseChange(
                nextState: fallbackState,
                evaluatedAt: cancelledAt);
        }

        public bool ExpireOverrideIfNeeded(
            SimTime evaluatedAt,
            WeatherState fallbackState)
        {
            GuardHelper.AgainstNull(
                value: fallbackState,
                propertyName: nameof(fallbackState));

            EnsureEvaluationTimeNotGoingBackwards(evaluatedAt);

            if (ActiveOverride is null)
                return false;

            if (!ActiveOverride.HasExpiredBy(evaluatedAt))
            {
                LastEvaluatedAt = evaluatedAt;
                return false;
            }

            ExpireOverrideInternal(
                evaluatedAt: evaluatedAt,
                fallbackState: fallbackState);

            return true;
        }

        public void ChangeClimateProfile(
            WeatherClimateProfile newClimateProfile,
            SimTime changedAt)
        {
            if (newClimateProfile is null)
                throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Climate profile is required.",
                    propertyName: nameof(newClimateProfile));

            EnsureEvaluationTimeNotGoingBackwards(changedAt);

            if (newClimateProfile == ClimateProfile)
            {
                LastEvaluatedAt = changedAt;
                return;
            }

            WeatherClimateProfile previousProfile = ClimateProfile;

            ClimateProfile = newClimateProfile;
            LastEvaluatedAt = changedAt;

            AddDomainEvent(
                new ClimateProfileChangedDomainEvent(
                    CityId: Id,
                    PreviousProfile: previousProfile,
                    CurrentProfile: newClimateProfile,
                    AtSimTime: changedAt));
        }

        private void ExpireOverrideInternal(
            SimTime evaluatedAt,
            WeatherState fallbackState)
        {
            WeatherOverride expiredOverride = ActiveOverride ??
                                              throw DomainErrorsFactory.NoActiveOverrideToCancel(
                                                  nameof(ActiveOverride));

            ActiveOverride = null;

            AddDomainEvent(
                new WeatherOverrideExpiredDomainEvent(
                    CityId: Id,
                    ForcedState: expiredOverride.ForcedState,
                    Source: expiredOverride.Source,
                    ExpiredAt: evaluatedAt));

            ApplyStateAndRaiseChange(
                nextState: fallbackState,
                evaluatedAt: evaluatedAt);
        }

        private void ApplyStateAndRaiseChange(
            WeatherState nextState,
            SimTime evaluatedAt)
        {
            EnsureStateIsApplicable(
                state: nextState,
                evaluatedAt: evaluatedAt);

            WeatherState previousState = CurrentState;

            LastEvaluatedAt = evaluatedAt;

            if (nextState == CurrentState)
                return;

            if (nextState.HasSameConditionsAs(CurrentState))
            {
                CurrentState = nextState;
                return;
            }

            CurrentState = nextState;
            LastTransitionAt = evaluatedAt;

            AddDomainEvent(
                new CityWeatherChangedDomainEvent(
                    CityId: Id,
                    PreviousState: previousState,
                    CurrentState: nextState,
                    AtSimTime: evaluatedAt));
        }

        private void EnsureEvaluationTimeNotGoingBackwards(SimTime evaluatedAt)
        {
            if (evaluatedAt.CompareTo(LastEvaluatedAt) < 0)
                throw DomainErrorsFactory.WeatherEvaluationTimeGoingBackwards(
                    value: evaluatedAt,
                    previous: LastEvaluatedAt,
                    propertyName: nameof(evaluatedAt));
        }

        private static void EnsureStateIsApplicable(
            WeatherState state,
            SimTime evaluatedAt)
        {
            if (!state.IsActiveAt(evaluatedAt))
                throw DomainErrorsFactory.InvalidWeatherTransitionTiming(
                    evaluatedAt: evaluatedAt,
                    startedAt: state.StartedAt,
                    expectedUntil: state.ExpectedUntil,
                    propertyName: nameof(state));
        }
    }
}
