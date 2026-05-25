using Matrix.Economy.Domain.Entities;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Entities
{
    public sealed class CityEconomyProgressionStateTests
    {
        [Fact]
        public void Create_WhenArgumentsAreValid_SetsProperties()
        {
            var cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            DateOnly lastProcessedDate = new(
                year: 2048,
                month: 2,
                day: 3);
            DateTimeOffset updatedAtUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);

            var state = CityEconomyProgressionState.Create(
                cityId: cityId,
                lastCompletedTickId: 12,
                lastProcessedDate: lastProcessedDate,
                updatedAtUtc: updatedAtUtc);

            Assert.Equal(
                expected: cityId,
                actual: state.CityId);
            Assert.Equal(
                expected: 12,
                actual: state.LastCompletedTickId);
            Assert.Equal(
                expected: lastProcessedDate,
                actual: state.LastProcessedDate);
            Assert.Equal(
                expected: updatedAtUtc,
                actual: state.UpdatedAtUtc);
        }

        [Fact]
        public void Create_WhenTickIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CityEconomyProgressionState.Create(
                cityId: Guid.NewGuid(),
                lastCompletedTickId: -1,
                lastProcessedDate: new DateOnly(
                    year: 2048,
                    month: 2,
                    day: 3),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero)));
        }

        [Fact]
        public void AdvanceProcessedDate_WhenDateMovesBackwards_ThrowsInvalidOperationException()
        {
            var state = CityEconomyProgressionState.Create(
                cityId: Guid.NewGuid(),
                lastCompletedTickId: 12,
                lastProcessedDate: new DateOnly(
                    year: 2048,
                    month: 2,
                    day: 3),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => state.AdvanceProcessedDate(
                    processedDate: new DateOnly(
                        year: 2048,
                        month: 2,
                        day: 2),
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 2,
                        day: 3,
                        hour: 6,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));

            Assert.Equal(
                expected: "Economy progression date cannot move backwards.",
                actual: exception.Message);
        }

        [Fact]
        public void MarkTickCompleted_WhenTickMovesForward_UpdatesState()
        {
            var state = CityEconomyProgressionState.Create(
                cityId: Guid.NewGuid(),
                lastCompletedTickId: 12,
                lastProcessedDate: new DateOnly(
                    year: 2048,
                    month: 2,
                    day: 3),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero));
            DateTimeOffset updatedAtUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 7,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            state.MarkTickCompleted(
                tickId: 15,
                updatedAtUtc: updatedAtUtc);

            Assert.Equal(
                expected: 15,
                actual: state.LastCompletedTickId);
            Assert.Equal(
                expected: updatedAtUtc,
                actual: state.UpdatedAtUtc);
        }

        [Fact]
        public void MarkTickCompleted_WhenTickMovesBackwards_ThrowsInvalidOperationException()
        {
            var state = CityEconomyProgressionState.Create(
                cityId: Guid.NewGuid(),
                lastCompletedTickId: 12,
                lastProcessedDate: new DateOnly(
                    year: 2048,
                    month: 2,
                    day: 3),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => state.MarkTickCompleted(
                    tickId: 11,
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 2,
                        day: 3,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));

            Assert.Equal(
                expected: "Economy progression tick cannot move backwards.",
                actual: exception.Message);
        }
    }
}
