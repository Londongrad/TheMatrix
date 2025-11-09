using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.BuildingBlocks.Domain
{
    /// <summary>
    /// Provides helper methods to enforce domain invariants and guard against invalid values.
    /// </summary>
    public static class GuardHelper
    {
        /// <summary>
        /// Ensures that the provided string is not null, empty, or whitespace.
        /// </summary>
        /// <param name="value">The string to validate.</param>
        /// <param name="propertyName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">Thrown when the string is null, empty, or whitespace.</exception>
        public static string AgainstNullOrEmpty(string? value, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainValidationException($"{propertyName} cannot be null or empty", propertyName);

            return value;
        }

        /// <summary>
        /// Ensures that the provided <see cref="Guid"/> is not empty.
        /// </summary>
        /// <param name="id">The GUID to validate.</param>
        /// <param name="propertyName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">Thrown when the GUID is empty.</exception>
        public static Guid AgainstEmptyGuid(Guid id, string propertyName)
        {
            if (id == Guid.Empty)
                throw new DomainValidationException($"{propertyName} cannot be empty Guid", propertyName);

            return id;
        }

        /// <summary>
        /// Ensures that the given object reference is not null.
        /// </summary>
        /// <typeparam name="T">The type of the object to validate.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="propertyName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">Thrown when the object is null.</exception>
        public static T AgainstNull<T>(T? value, string propertyName) where T : class
        {
            if (value is null)
                throw new DomainValidationException($"{propertyName} cannot be null", propertyName);

            return value;
        }

        /// <summary>
        /// Ensures that the provided enum value is defined in the enumeration.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
        /// <param name="value">The enum value to validate.</param>
        /// <param name="propertyName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">Thrown when the enum value is not defined.</exception>
        public static TEnum AgainstInvalidEnum<TEnum>(TEnum value, string propertyName) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw new DomainValidationException($"{propertyName} has invalid value: {value}", propertyName);

            return value;
        }

        /// <summary>
        /// Ensures that the provided string can be successfully parsed into a valid enum value.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
        /// <param name="value">The string to parse.</param>
        /// <param name="propertyName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">Thrown when the string cannot be parsed into a valid enum value.</exception>
        public static TEnum AgainstInvalidStringToEnum<TEnum>(string value, string propertyName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, true, out TEnum newEnum))
                throw new DomainValidationException($"{propertyName} has invalid value: {value}", propertyName);

            return newEnum;
        }

        /// <summary>
        /// Throws a <see cref="DomainValidationException"/> if the specified numeric value is less than or equal to zero.
        /// </summary>
        /// <typeparam name="T">A numeric value type that implements <see cref="IComparable{T}"/> (e.g., int, double, decimal).</typeparam>
        /// <param name="value">The numeric value to validate.</param>
        /// <param name="propertyName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">
        /// Thrown when <paramref name="value"/> is less than or equal to zero.
        /// </exception>
        public static T AgainstNonPositiveNumber<T>(T value, string propertyName)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(default) <= 0)
                throw new DomainValidationException($"{propertyName} should be positive", propertyName);

            return value;
        }

        public static T AgainstNegativeNumber<T>(T value, string propertyName)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(default) < 0)
                throw new DomainValidationException($"{propertyName} should not be negative", propertyName);

            return value;
        }

        public static T AgainstOutOfRange<T>(
            T value,
            T min,
            T max,
            string propertyName) where T : struct, IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                throw new DomainValidationException($"{propertyName} must be between {min} and {max}.",
                    propertyName);
            }

            return value;
        }
    }
}
