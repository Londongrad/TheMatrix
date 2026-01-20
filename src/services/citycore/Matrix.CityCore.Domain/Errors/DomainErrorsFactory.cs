using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.CityCore.Domain.Cities;

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

        #endregion [ Cities ]
    }
}
