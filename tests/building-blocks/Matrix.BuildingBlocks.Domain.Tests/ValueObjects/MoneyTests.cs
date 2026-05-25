using Matrix.BuildingBlocks.Domain.ValueObjects;
using Xunit;

namespace Matrix.BuildingBlocks.Domain.Tests.ValueObjects
{
    public sealed class MoneyTests
    {
        [Fact]
        public void FromDecimal_WhenAmountNeedsScaling_RoundsAwayFromZero()
        {
            var positive = Money.FromDecimal(12.345m);
            var negative = Money.FromDecimal(-12.345m);

            Assert.Equal(
                expected: 12.35m,
                actual: positive.Amount);
            Assert.Equal(
                expected: -12.35m,
                actual: negative.Amount);
            Assert.False(positive.IsZero);
            Assert.True(negative.IsNegative);
        }

        [Fact]
        public void ArithmeticOperations_WhenApplied_ReturnNormalizedResults()
        {
            var left = Money.FromDecimal(10.12m);
            var right = Money.FromDecimal(2.235m);

            Money sum = left.Add(right);
            Money difference = left.Subtract(right);
            Money multiplied = right.Multiply(1.5m);

            Assert.Equal(
                expected: 12.36m,
                actual: sum.Amount);
            Assert.Equal(
                expected: 7.88m,
                actual: difference.Amount);
            Assert.Equal(
                expected: 3.36m,
                actual: multiplied.Amount);
        }

        [Fact]
        public void ComparisonAndFormatting_WhenAmountsMatch_UseNormalizedValue()
        {
            var left = Money.FromDecimal(5m);
            var right = Money.FromDecimal(5.004m);
            var larger = Money.FromDecimal(5.01m);

            Assert.Equal(
                expected: left,
                actual: right);
            Assert.True(larger > left);
            Assert.Equal(
                expected: "5.00",
                actual: right.ToString());
        }
    }
}
