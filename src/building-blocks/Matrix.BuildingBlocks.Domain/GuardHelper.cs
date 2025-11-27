using Matrix.BuildingBlocks.Domain.Errors;

namespace Matrix.BuildingBlocks.Domain
{
    /// <summary>
    /// Provides helper methods to enforce domain invariants and guard against invalid values.
    /// </summary>
    public static class GuardHelper
    {
        public static string AgainstNullOrEmpty(string? value, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw DomainErrors.NullOrEmpty(propertyName);

            return value;
        }

        public static Guid AgainstEmptyGuid(Guid id, string propertyName)
        {
            if (id == Guid.Empty)
                throw DomainErrors.EmptyGuid(propertyName);

            return id;
        }

        public static T AgainstNull<T>(T? value, string propertyName) where T : class
        {
            if (value is null)
                throw DomainErrors.Null(propertyName);

            return value;
        }

        public static TEnum AgainstInvalidEnum<TEnum>(TEnum value, string propertyName)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw DomainErrors.InvalidEnum(value, propertyName);

            return value;
        }

        public static TEnum AgainstInvalidStringToEnum<TEnum>(string value, string propertyName)
            where TEnum : struct, Enum
        {
            if (!Enum.TryParse(value, true, out TEnum newEnum))
                throw DomainErrors.InvalidStringToEnum<TEnum>(value, propertyName);

            return newEnum;
        }

        public static T AgainstNonPositiveNumber<T>(T value, string propertyName)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(default) <= 0)
                throw DomainErrors.NonPositiveNumber(propertyName);

            return value;
        }

        public static T AgainstNegativeNumber<T>(T value, string propertyName)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(default) < 0)
                throw DomainErrors.NegativeNumber(propertyName);

            return value;
        }

        public static T AgainstOutOfRange<T>(T value, T min, T max, string propertyName)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                throw DomainErrors.OutOfRange(min, max, propertyName);

            return value;
        }

        public static void AgainstInvalidDateOnly(DateOnly? value, string argumentName)
        {
            if (value is null)
                return;

            if (value.Value == default)
                throw DomainErrors.DefaultDateOnly(argumentName);

            if (value.Value < DateOnly.FromDateTime(new DateTime(0001, 1, 1)))
                throw DomainErrors.DateTooFarInPast(argumentName);
        }

        public static void AgainstInvalidDateRange(DateOnly from, DateOnly? to)
        {
            if (to is null)
                return;

            AgainstInvalidDateOnly(from, nameof(from));
            AgainstInvalidDateOnly(to, nameof(to));

            if (from > to.Value)
                throw DomainErrors.InvalidDateRange(from, to.Value, nameof(from), nameof(to));
        }
    }
}
