using Matrix.BuildingBlocks.Domain.ValueObjects;
using Xunit;

namespace Matrix.BuildingBlocks.Domain.Tests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void FromDecimal_WhenAmountNeedsScaling_RoundsAwayFromZero()
    {
        Money positive = Money.FromDecimal(12.345m);
        Money negative = Money.FromDecimal(-12.345m);

        Assert.Equal(12.35m, positive.Amount);
        Assert.Equal(-12.35m, negative.Amount);
        Assert.False(positive.IsZero);
        Assert.True(negative.IsNegative);
    }

    [Fact]
    public void ArithmeticOperations_WhenApplied_ReturnNormalizedResults()
    {
        Money left = Money.FromDecimal(10.12m);
        Money right = Money.FromDecimal(2.235m);

        Money sum = left.Add(right);
        Money difference = left.Subtract(right);
        Money multiplied = right.Multiply(1.5m);

        Assert.Equal(12.36m, sum.Amount);
        Assert.Equal(7.88m, difference.Amount);
        Assert.Equal(3.36m, multiplied.Amount);
    }

    [Fact]
    public void ComparisonAndFormatting_WhenAmountsMatch_UseNormalizedValue()
    {
        Money left = Money.FromDecimal(5m);
        Money right = Money.FromDecimal(5.004m);
        Money larger = Money.FromDecimal(5.01m);

        Assert.Equal(left, right);
        Assert.True(larger > left);
        Assert.Equal("5.00", right.ToString());
    }
}
