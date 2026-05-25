using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class CityUtcOffsetTests
    {
        private const string OutOfRangeErrorCode = "SimulationCore.City.UtcOffset.OutOfRange";
        private const string InvalidStepErrorCode = "SimulationCore.City.UtcOffset.InvalidStep";

        [Fact]
        public void Constructor_AcceptsBoundaryValues()
        {
            var min = new CityUtcOffset(TimeSpan.FromMinutes(CityUtcOffset.MinMinutes));
            var max = new CityUtcOffset(TimeSpan.FromMinutes(CityUtcOffset.MaxMinutes));

            Assert.Equal(
                expected: CityUtcOffset.MinMinutes,
                actual: min.TotalMinutes);
            Assert.Equal(
                expected: CityUtcOffset.MaxMinutes,
                actual: max.TotalMinutes);
        }

        [Fact]
        public void FromMinutes_CreatesOffset()
        {
            var offset = CityUtcOffset.FromMinutes(330);

            Assert.Equal(
                expected: TimeSpan.FromMinutes(330),
                actual: offset.Value);
            Assert.Equal(
                expected: 330,
                actual: offset.TotalMinutes);
        }

        [Fact]
        public void Constructor_WhenMinutesDoNotAlignToStep_ThrowsDomainException()
        {
            DomainException exception =
                Assert.Throws<DomainException>(() => new CityUtcOffset(TimeSpan.FromMinutes(10)));

            Assert.Equal(
                expected: InvalidStepErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Constructor_WhenValueIsBelowMinimum_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() =>
                new CityUtcOffset(TimeSpan.FromMinutes(CityUtcOffset.MinMinutes - CityUtcOffset.StepMinutes)));

            Assert.Equal(
                expected: OutOfRangeErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Constructor_WhenValueIsAboveMaximum_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() =>
                new CityUtcOffset(TimeSpan.FromMinutes(CityUtcOffset.MaxMinutes + CityUtcOffset.StepMinutes)));

            Assert.Equal(
                expected: OutOfRangeErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void ToString_FormatsPositiveAndNegativeOffsets()
        {
            var positive = CityUtcOffset.FromMinutes(330);
            var negative = CityUtcOffset.FromMinutes(-180);

            Assert.Equal(
                expected: "+05:30",
                actual: positive.ToString());
            Assert.Equal(
                expected: "-03:00",
                actual: negative.ToString());
        }
    }
}
