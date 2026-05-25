using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation
{
    public sealed class SimSpeedTests
    {
        private const string MultiplierOutOfRangeErrorCode = "SimulationCore.SimSpeed.Multiplier.OutOfRange";
        private const string RealDeltaNotPositiveErrorCode = "SimulationCore.SimSpeed.RealDelta.NotPositive";

        [Fact]
        public void RealTime_ReturnsMultiplierOfOne()
        {
            var speed = SimSpeed.RealTime();

            Assert.Equal(
                expected: 1.0m,
                actual: speed.Multiplier);
        }

        [Fact]
        public void From_AcceptsBoundaryValues()
        {
            var min = SimSpeed.From(SimSpeed.Min);
            var max = SimSpeed.From(SimSpeed.Max);

            Assert.Equal(
                expected: SimSpeed.Min,
                actual: min.Multiplier);
            Assert.Equal(
                expected: SimSpeed.Max,
                actual: max.Multiplier);
        }

        [Fact]
        public void From_WhenBelowMin_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => SimSpeed.From(SimSpeed.Min - 0.0001m));

            Assert.Equal(
                expected: MultiplierOutOfRangeErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void From_WhenAboveMax_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => SimSpeed.From(SimSpeed.Max + 0.0001m));

            Assert.Equal(
                expected: MultiplierOutOfRangeErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Apply_ScalesTime_UsingTickRoundingAwayFromZero()
        {
            TimeSpan scaled = SimSpeed.From(1.5m)
               .Apply(TimeSpan.FromTicks(1));

            Assert.Equal(
                expected: TimeSpan.FromTicks(2),
                actual: scaled);
        }

        [Fact]
        public void Apply_WithZeroDelta_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => SimSpeed.RealTime()
               .Apply(TimeSpan.Zero));

            Assert.Equal(
                expected: RealDeltaNotPositiveErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Apply_WithNegativeDelta_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => SimSpeed.RealTime()
               .Apply(TimeSpan.FromSeconds(-1)));

            Assert.Equal(
                expected: RealDeltaNotPositiveErrorCode,
                actual: exception.Code);
        }
    }
}
