using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation
{
    public sealed class SimTimeTests
    {
        private const string SimTimeNotUtcErrorCode = "SimulationCore.SimTime.NotUtc";

        [Fact]
        public void FromUtc_AcceptsUtcDateTimeOffset()
        {
            var value = new DateTimeOffset(
                year: 2035,
                month: 6,
                day: 7,
                hour: 8,
                minute: 9,
                second: 10,
                offset: TimeSpan.Zero);

            var simTime = SimTime.FromUtc(value);

            Assert.Equal(
                expected: value,
                actual: simTime.ValueUtc);
        }

        [Fact]
        public void FromUtc_WhenOffsetIsNotZero_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => SimTime.FromUtc(
                new DateTimeOffset(
                    year: 2035,
                    month: 6,
                    day: 7,
                    hour: 8,
                    minute: 9,
                    second: 10,
                    offset: TimeSpan.FromHours(3))));

            Assert.Equal(
                expected: SimTimeNotUtcErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Add_ShiftsTime_AndPreservesUtcOffset()
        {
            var start = SimTime.FromUtc(
                new DateTimeOffset(
                    year: 2035,
                    month: 6,
                    day: 7,
                    hour: 8,
                    minute: 9,
                    second: 10,
                    offset: TimeSpan.Zero));

            SimTime result = start.Add(TimeSpan.FromMinutes(90));

            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2035,
                    month: 6,
                    day: 7,
                    hour: 9,
                    minute: 39,
                    second: 10,
                    offset: TimeSpan.Zero),
                actual: result.ValueUtc);
            Assert.Equal(
                expected: TimeSpan.Zero,
                actual: result.ValueUtc.Offset);
        }
    }
}
