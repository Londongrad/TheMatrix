using System.Globalization;

namespace Matrix.BuildingBlocks.Domain.ValueObjects
{
    public sealed class Money(decimal amount)
    {
        public decimal Amount { get; } = amount;

        public static Money Zero => new(0m);

        public static Money FromDecimal(decimal amount)
        {
            return new Money(amount);
        }

        public Money Add(Money other)
        {
            return new Money(Amount + other.Amount);
        }

        public Money Subtract(Money other)
        {
            return new Money(Amount - other.Amount);
        }

        public override string ToString()
        {
            return Amount.ToString(
                format: "F2",
                provider: CultureInfo.InvariantCulture);
        }
    }
}
