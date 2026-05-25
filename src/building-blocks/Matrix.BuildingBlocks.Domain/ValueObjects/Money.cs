using System.Globalization;

namespace Matrix.BuildingBlocks.Domain.ValueObjects
{
    /// <summary>
    ///     Represents the fixed-scale single-currency money amount used by the current city economy.
    ///     Negative values are allowed because the simulation models deficits, arrears and debt-like states.
    ///     Other simulations with different exchange units should define their own value objects
    ///     instead of overloading this type.
    /// </summary>
    public sealed class Money(decimal amount) : IEquatable<Money>, IComparable<Money>
    {
        public const int Scale = 2;
        public const MidpointRounding ScaleRoundingMode = MidpointRounding.AwayFromZero;

        public decimal Amount { get; } = Normalize(amount);

        public static Money Zero { get; } = new(0m);

        public bool IsZero => Amount == 0m;
        public bool IsPositive => Amount > 0m;
        public bool IsNegative => Amount < 0m;

        public int CompareTo(Money? other)
        {
            if (other is null)
                return 1;

            return Amount.CompareTo(other.Amount);
        }

        public bool Equals(Money? other)
        {
            return other is not null && Amount == other.Amount;
        }

        public static Money FromDecimal(decimal amount)
        {
            return new Money(amount);
        }

        public Money Add(Money other)
        {
            ArgumentNullException.ThrowIfNull(other);

            return new Money(Amount + other.Amount);
        }

        public Money Subtract(Money other)
        {
            ArgumentNullException.ThrowIfNull(other);

            return new Money(Amount - other.Amount);
        }

        public Money Multiply(decimal factor)
        {
            return new Money(Amount * factor);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(
                       objA: this,
                       objB: obj) ||
                   (obj is Money other && Equals(other));
        }

        public override int GetHashCode()
        {
            return Amount.GetHashCode();
        }

        public static bool operator ==(
            Money? left,
            Money? right)
        {
            return Equals(
                objA: left,
                objB: right);
        }

        public static bool operator !=(
            Money? left,
            Money? right)
        {
            return !Equals(
                objA: left,
                objB: right);
        }

        public static bool operator <(
            Money? left,
            Money? right)
        {
            return CompareNullable(
                       left: left,
                       right: right) <
                   0;
        }

        public static bool operator <=(
            Money? left,
            Money? right)
        {
            return CompareNullable(
                       left: left,
                       right: right) <=
                   0;
        }

        public static bool operator >(
            Money? left,
            Money? right)
        {
            return CompareNullable(
                       left: left,
                       right: right) >
                   0;
        }

        public static bool operator >=(
            Money? left,
            Money? right)
        {
            return CompareNullable(
                       left: left,
                       right: right) >=
                   0;
        }

        public override string ToString()
        {
            return Amount.ToString(
                format: $"F{Scale}",
                provider: CultureInfo.InvariantCulture);
        }

        private static int CompareNullable(
            Money? left,
            Money? right)
        {
            if (ReferenceEquals(
                    objA: left,
                    objB: right))
                return 0;

            if (left is null)
                return -1;

            return left.CompareTo(right);
        }

        private static decimal Normalize(decimal amount)
        {
            return decimal.Round(
                d: amount,
                decimals: Scale,
                mode: ScaleRoundingMode);
        }
    }
}
