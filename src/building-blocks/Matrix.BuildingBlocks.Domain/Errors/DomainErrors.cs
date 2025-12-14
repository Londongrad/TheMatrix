using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.BuildingBlocks.Domain.Errors
{
    public static class DomainErrors
    {
        public static DomainException NullOrEmpty(string propertyName)
        {
            return new DomainException(
                code: "Domain.Guard.NullOrEmpty",
                message: $"{propertyName} cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException EmptyGuid(string propertyName)
        {
            return new DomainException(
                code: "Domain.Guard.EmptyGuid",
                message: $"{propertyName} cannot be empty Guid.",
                propertyName: propertyName);
        }

        public static DomainException Null(string propertyName)
        {
            return new DomainException(
                code: "Domain.Guard.Null",
                message: $"{propertyName} cannot be null.",
                propertyName: propertyName);
        }

        public static DomainException InvalidEnum<TEnum>(
            TEnum value,
            string propertyName)
            where TEnum : struct, Enum
        {
            return new DomainException(
                code: "Domain.Guard.InvalidEnum",
                message: $"{propertyName} has invalid value: {value}.",
                propertyName: propertyName);
        }

        public static DomainException InvalidStringToEnum<TEnum>(
            string value,
            string propertyName)
            where TEnum : struct, Enum
        {
            return new DomainException(
                code: "Domain.Guard.InvalidStringToEnum",
                message: $"{propertyName} has invalid value: {value}.",
                propertyName: propertyName);
        }

        public static DomainException NonPositiveNumber(string propertyName)
        {
            return new DomainException(
                code: "Domain.Guard.NonPositiveNumber",
                message: $"{propertyName} should be positive.",
                propertyName: propertyName);
        }

        public static DomainException NegativeNumber(string propertyName)
        {
            return new DomainException(
                code: "Domain.Guard.NegativeNumber",
                message: $"{propertyName} should not be negative.",
                propertyName: propertyName);
        }

        public static DomainException OutOfRange<T>(
            T min,
            T max,
            string propertyName)
            where T : struct, IComparable<T>
        {
            return new DomainException(
                code: "Domain.Guard.OutOfRange",
                message: $"{propertyName} must be between {min} and {max}.",
                propertyName: propertyName);
        }

        public static DomainException DefaultDateOnly(string propertyName)
        {
            return new DomainException(
                code: "Domain.Guard.DefaultDateOnly",
                message: $"{propertyName} cannot be default.",
                propertyName: propertyName);
        }

        public static DomainException DateTooFarInPast(string propertyName)
        {
            return new DomainException(
                code: "Domain.Guard.DateTooFarInPast",
                message: $"{propertyName} is too far in the past.",
                propertyName: propertyName);
        }

        public static DomainException InvalidDateRange(
            DateOnly from,
            DateOnly to,
            string fromName,
            string toName)
        {
            return new DomainException(
                code: "Domain.Guard.InvalidDateRange",
                message: $"Invalid date range: {fromName} {from} cannot be later than {toName} {to}.",
                propertyName: null);
        }
    }
}
