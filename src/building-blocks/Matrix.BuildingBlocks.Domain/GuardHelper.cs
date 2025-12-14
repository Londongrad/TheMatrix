using System.Runtime.CompilerServices;
using Matrix.BuildingBlocks.Domain.Errors;
using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.BuildingBlocks.Domain
{
    /// <summary>
    ///     Provides helper methods to enforce domain invariants and guard against invalid values.
    /// </summary>
    public static class GuardHelper
    {
        #region [ Conditions ]

        public static void Ensure<T>(
            bool condition,
            T value,
            Func<T, string?, DomainException> errorFactory,
            [CallerArgumentExpression("value")] string? propertyName = null)
            where T : struct
        {
            if (!condition)
                throw errorFactory(value, propertyName);
        }

        #endregion [ Conditions ]

        #region [ NullOrEmpty ]

        public static string AgainstNullOrWhiteSpace(
            string? value,
            string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw DomainErrors.NullOrEmpty(propertyName);

            return value;
        }

        public static string AgainstNullOrWhiteSpace(
            string? value,
            Func<string?, DomainException> errorFactory,
            bool trim = true,
            [CallerArgumentExpression("value")] string? propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw errorFactory(propertyName);

            return trim ? value.Trim() : value;
        }

        public static Guid AgainstEmptyGuid(
            Guid id,
            string propertyName)
        {
            if (id == Guid.Empty)
                throw DomainErrors.EmptyGuid(propertyName);

            return id;
        }

        public static Guid AgainstEmptyGuid(
            Guid id,
            Func<string?, DomainException> errorFactory,
            [CallerArgumentExpression("id")] string? propertyName = null)
        {
            if (id == Guid.Empty)
                throw errorFactory(propertyName);

            return id;
        }

        public static T AgainstNull<T>(
            T? value,
            string propertyName)
            where T : class
        {
            if (value is null)
                throw DomainErrors.Null(propertyName);

            return value;
        }

        public static void AgainstNull<T>(
            T? value,
            Func<T, string?, DomainException> errorFactory,
            [CallerArgumentExpression("value")] string? propertyName = null)
            where T : struct
        {
            if (value is not null)
                throw errorFactory(value.Value, propertyName);
        }

        #endregion [ NullOrEmpty ]

        #region [ Enums ]

        public static TEnum AgainstInvalidEnum<TEnum>(
            TEnum value,
            string propertyName)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw DomainErrors.InvalidEnum(
                    value: value,
                    propertyName: propertyName);

            return value;
        }

        public static TEnum AgainstInvalidEnum<TEnum>(
            TEnum value,
            Func<TEnum, string?, DomainException> errorFactory,
            [CallerArgumentExpression("value")] string? propertyName = null)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw errorFactory(
                    arg1: value,
                    arg2: propertyName);

            return value;
        }

        public static TEnum AgainstInvalidEnum<TEnum>(
            TEnum value,
            Func<string?, DomainException> errorFactory,
            [CallerArgumentExpression("value")] string? propertyName = null)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw errorFactory(propertyName);

            return value;
        }

        public static TEnum AgainstInvalidStringToEnum<TEnum>(
            string value,
            string propertyName)
            where TEnum : struct, Enum
        {
            if (!Enum.TryParse(
                    value: value,
                    ignoreCase: true,
                    result: out TEnum newEnum))
                throw DomainErrors.InvalidStringToEnum<TEnum>(
                    value: value,
                    propertyName: propertyName);

            return newEnum;
        }

        #endregion [ Enums ]

        #region [ Numbers ]

        public static T AgainstNonPositiveNumber<T>(
            T value,
            string propertyName)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(default(T)) <= 0)
                throw DomainErrors.NonPositiveNumber(propertyName);

            return value;
        }

        public static T AgainstNegativeNumber<T>(
            T value,
            string propertyName)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(default(T)) < 0)
                throw DomainErrors.NegativeNumber(propertyName);

            return value;
        }

        public static T AgainstOutOfRange<T>(
            T value,
            T min,
            T max,
            string propertyName)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                throw DomainErrors.OutOfRange(
                    min: min,
                    max: max,
                    propertyName: propertyName);

            return value;
        }

        public static T AgainstOutOfRange<T>(
            T value,
            T min,
            T max,
            Func<T, T, T, string?, DomainException> errorFactory,
            [CallerArgumentExpression("value")] string? propertyName = null)
            where T : struct, IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                throw errorFactory(
                    arg1: value,
                    arg2: min,
                    arg3: max,
                    arg4: propertyName);

            return value;
        }

        #endregion [ Numbers ]

        #region [ Dates ]

        public static void AgainstInvalidDateOnly(
            DateOnly? value,
            string argumentName)
        {
            if (value is null)
                return;

            if (value.Value == default(DateOnly))
                throw DomainErrors.DefaultDateOnly(argumentName);

            if (value.Value <
                DateOnly.FromDateTime(
                    new DateTime(
                        year: 0001,
                        month: 1,
                        day: 1)))
                throw DomainErrors.DateTooFarInPast(argumentName);
        }

        public static void AgainstInvalidDateRange(
            DateOnly from,
            DateOnly? to)
        {
            if (to is null)
                return;

            AgainstInvalidDateOnly(
                value: from,
                argumentName: nameof(from));

            AgainstInvalidDateOnly(
                value: to,
                argumentName: nameof(to));

            if (from > to.Value)
                throw DomainErrors.InvalidDateRange(
                    from: from,
                    to: to.Value,
                    fromName: nameof(from),
                    toName: nameof(to));
        }

        #endregion [ Dates ]
    }
}
