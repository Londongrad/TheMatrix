using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Healthcare.Domain.Patients;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Patients
{
    public sealed class HealthScoreTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Constructor_WhenValueIsOutsideRange_ThrowsDomainException(int value)
        {
            Assert.Throws<DomainException>(() => new HealthScore(value));
        }

        [Theory]
        [InlineData(80, 15, 95)]
        [InlineData(80, 30, 100)]
        [InlineData(20, -15, 5)]
        [InlineData(20, -30, 0)]
        public void ApplyDelta_ClampsValueToValidRange(int initial, int delta, int expected)
        {
            HealthScore result = new HealthScore(initial).ApplyDelta(delta);

            Assert.Equal(expected, result.Value);
        }
    }
}
