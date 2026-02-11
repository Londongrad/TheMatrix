using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather
{
    /// <summary>
    ///     Active override that forces a city weather state for a bounded time window.
    /// </summary>
    public sealed class WeatherOverride : Entity<Guid>
    {
        private WeatherOverride(
            Guid id,
            WeatherState forcedState,
            WeatherOverrideSource source,
            string? reason,
            SimTime startsAt,
            SimTime endsAt)
            : base(id)
        {
            ForcedState = forcedState;
            Source = source;
            Reason = reason;
            StartsAt = startsAt;
            EndsAt = endsAt;
        }

        private WeatherOverride()
            : base(Guid.Empty)
        {
            ForcedState = null!;
        }

        public WeatherState ForcedState { get; private set; }
        public WeatherOverrideSource Source { get; private set; }
        public string? Reason { get; private set; }
        public SimTime StartsAt { get; }
        public SimTime EndsAt { get; }

        public static WeatherOverride Create(
            WeatherState forcedState,
            WeatherOverrideSource source,
            string? reason = null)
        {
            GuardHelper.AgainstNull(
                value: forcedState,
                propertyName: nameof(forcedState));
            GuardHelper.AgainstInvalidEnum(
                value: source,
                propertyName: nameof(Source));

            SimTime startsAt = forcedState.StartedAt;
            SimTime endsAt = forcedState.ExpectedUntil;

            if (endsAt.CompareTo(startsAt) <= 0)
                throw ClassicCityDomainErrorsFactory.InvalidOverrideTimeRange(
                    startsAt: startsAt,
                    endsAt: endsAt,
                    propertyName: nameof(EndsAt));

            string? normalizedReason = string.IsNullOrWhiteSpace(reason)
                ? null
                : reason.Trim();

            return new WeatherOverride(
                id: Guid.NewGuid(),
                forcedState: forcedState,
                source: source,
                reason: normalizedReason,
                startsAt: startsAt,
                endsAt: endsAt);
        }

        public bool IsActiveAt(SimTime time)
        {
            return time.CompareTo(StartsAt) >= 0 &&
                   time.CompareTo(EndsAt) < 0;
        }

        public bool HasExpiredBy(SimTime time)
        {
            return time.CompareTo(EndsAt) >= 0;
        }
    }
}
