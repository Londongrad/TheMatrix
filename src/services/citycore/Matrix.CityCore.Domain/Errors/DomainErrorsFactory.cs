using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather.Enums;

namespace Matrix.CityCore.Domain.Errors
{
    public static class DomainErrorsFactory
    {
        #region [ Simulation ]

        public static DomainException SimTimeMustBeUtc(
            DateTimeOffset value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.SimTime.NotUtc",
                message: "SimTime must be in UTC (Offset=00:00).",
                propertyName: propertyName);
        }

        public static DomainException SimSpeedMultiplierOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.SimSpeed.Multiplier.OutOfRange",
                message: $"SimSpeed multiplier must be in range [{min}; {max}].",
                propertyName: propertyName);
        }

        public static DomainException SimSpeedRealDeltaMustBePositive(
            TimeSpan value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.SimSpeed.RealDelta.NotPositive",
                message: "realDelta must be positive.",
                propertyName: propertyName);
        }

        #endregion [ Simulation ]

        #region [ Cities ]

        public static DomainException CityNameNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.City.Name.NullOrEmpty",
                message: "City name cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException CityNameTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.City.Name.TooLong",
                message: $"City name cannot be longer than {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException CityTimestampMustBeUtc(
            DateTimeOffset value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.City.Timestamp.NotUtc",
                message: "City timestamps must be in UTC (Offset=00:00).",
                propertyName: propertyName);
        }

        public static DomainException CityIsArchived(
            CityStatus value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.City.Archived",
                message: "Operation is not allowed for an archived city.",
                propertyName: propertyName);
        }

        public static DomainException InvalidCityEnvironment(
            string reason,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.City.Environment.Invalid",
                message: $"City environment is invalid. {reason}",
                propertyName: propertyName);
        }

        public static DomainException CityUtcOffsetOutOfRange(
            int valueMinutes,
            int minMinutes,
            int maxMinutes,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.City.UtcOffset.OutOfRange",
                message: $"City UTC offset must be in range [{minMinutes}; {maxMinutes}] minutes.",
                propertyName: propertyName);
        }

        public static DomainException CityUtcOffsetMustAlignToStep(
            int valueMinutes,
            int stepMinutes,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.City.UtcOffset.InvalidStep",
                message: $"City UTC offset must align to {stepMinutes}-minute increments.",
                propertyName: propertyName);
        }

        #endregion [ Cities ]

        #region [ Weather ]

        public static DomainException TemperatureCOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.Temperature.OutOfRange",
                message: $"Temperature must be in range [{min}; {max}] degrees Celsius.",
                propertyName: propertyName);
        }

        public static DomainException HumidityPercentOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.Humidity.OutOfRange",
                message: "Humidity must be in range [0; 100] percent.",
                propertyName: propertyName);
        }

        public static DomainException WindSpeedKphOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.WindSpeed.OutOfRange",
                message: $"Wind speed must be in range [{min}; {max}] kph.",
                propertyName: propertyName);
        }

        public static DomainException CloudCoveragePercentOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.CloudCoverage.OutOfRange",
                message: "Cloud coverage must be in range [0; 100] percent.",
                propertyName: propertyName);
        }

        public static DomainException PressureHpaOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.Pressure.OutOfRange",
                message: $"Pressure must be in range [{min}; {max}] hPa.",
                propertyName: propertyName);
        }

        public static DomainException WeatherVolatilityOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.Volatility.OutOfRange",
                message: "Weather volatility must be in range [0; 1].",
                propertyName: propertyName);
        }

        public static DomainException InvalidWeatherStateTimeRange(
            SimTime startedAt,
            SimTime expectedUntil,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.State.TimeRange.Invalid",
                message: $"Weather state ExpectedUntil ({expectedUntil}) must be greater than StartedAt ({startedAt}).",
                propertyName: propertyName);
        }

        public static DomainException InvalidOverrideTimeRange(
            SimTime startsAt,
            SimTime endsAt,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.Override.TimeRange.Invalid",
                message: $"Weather override EndsAt ({endsAt}) must be greater than StartsAt ({startsAt}).",
                propertyName: propertyName);
        }

        public static DomainException OverrideAlreadyActive(string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.Override.AlreadyActive",
                message: "Only one active weather override is allowed per city.",
                propertyName: propertyName);
        }

        public static DomainException NoActiveOverrideToCancel(string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.Override.NotActive",
                message: "There is no active weather override to cancel or expire.",
                propertyName: propertyName);
        }

        public static DomainException WeatherEvaluationTimeGoingBackwards(
            SimTime value,
            SimTime previous,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.Evaluation.Time.Backwards",
                message: $"Weather evaluation time ({value}) cannot be earlier than the last evaluated time ({previous}).",
                propertyName: propertyName);
        }

        public static DomainException InvalidClimateProfile(
            string reason,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.ClimateProfile.Invalid",
                message: $"Weather climate profile is invalid. {reason}",
                propertyName: propertyName);
        }

        public static DomainException InvalidWeatherTransitionTiming(
            SimTime evaluatedAt,
            SimTime startedAt,
            SimTime expectedUntil,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.Transition.Timing.Invalid",
                message: $"Weather state must be active at evaluation time ({evaluatedAt}); active range is [{startedAt}; {expectedUntil}).",
                propertyName: propertyName);
        }

        public static DomainException IncoherentWeatherPrecipitation(
            WeatherType type,
            PrecipitationKind precipitationKind,
            string? propertyName = null)
        {
            return new DomainException(
                code: "CityCore.Weather.Precipitation.Incoherent",
                message: $"Precipitation kind '{precipitationKind}' is not coherent with weather type '{type}'.",
                propertyName: propertyName);
        }

        #endregion [ Weather ]
    }
}