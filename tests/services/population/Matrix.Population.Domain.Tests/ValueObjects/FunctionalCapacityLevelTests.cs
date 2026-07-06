using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.ValueObjects
{
    public sealed class FunctionalCapacityLevelTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(47)]
        [InlineData(100)]
        public void From_ValidScore_PreservesValue(int score)
        {
            FunctionalCapacityLevel level = FunctionalCapacityLevel.From(score);

            Assert.Equal(score, level.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void From_OutOfRangeScore_ThrowsArgumentOutOfRangeException(int score)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                FunctionalCapacityLevel.From(score));
        }
    }
}
