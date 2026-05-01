using Matrix.Economy.Domain.Entities;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Entities;

public sealed class CityEconomyProgressionStateTests
{
    [Fact]
    public void Create_WhenArgumentsAreValid_SetsProperties()
    {
        Guid cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        DateOnly lastProcessedDate = new(2048, 2, 3);
        DateTimeOffset updatedAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);

        CityEconomyProgressionState state = CityEconomyProgressionState.Create(
            cityId: cityId,
            lastCompletedTickId: 12,
            lastProcessedDate: lastProcessedDate,
            updatedAtUtc: updatedAtUtc);

        Assert.Equal(cityId, state.CityId);
        Assert.Equal(12, state.LastCompletedTickId);
        Assert.Equal(lastProcessedDate, state.LastProcessedDate);
        Assert.Equal(updatedAtUtc, state.UpdatedAtUtc);
    }

    [Fact]
    public void Create_WhenTickIsNegative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CityEconomyProgressionState.Create(
                cityId: Guid.NewGuid(),
                lastCompletedTickId: -1,
                lastProcessedDate: new DateOnly(2048, 2, 3),
                updatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero)));
    }

    [Fact]
    public void AdvanceProcessedDate_WhenDateMovesBackwards_ThrowsInvalidOperationException()
    {
        CityEconomyProgressionState state = CityEconomyProgressionState.Create(
            cityId: Guid.NewGuid(),
            lastCompletedTickId: 12,
            lastProcessedDate: new DateOnly(2048, 2, 3),
            updatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => state.AdvanceProcessedDate(
                processedDate: new DateOnly(2048, 2, 2),
                updatedAtUtc: new DateTimeOffset(2048, 2, 3, 6, 0, 0, TimeSpan.Zero)));

        Assert.Equal("Economy progression date cannot move backwards.", exception.Message);
    }

    [Fact]
    public void MarkTickCompleted_WhenTickMovesForward_UpdatesState()
    {
        CityEconomyProgressionState state = CityEconomyProgressionState.Create(
            cityId: Guid.NewGuid(),
            lastCompletedTickId: 12,
            lastProcessedDate: new DateOnly(2048, 2, 3),
            updatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero));
        DateTimeOffset updatedAtUtc = new(2048, 2, 3, 7, 0, 0, TimeSpan.Zero);

        state.MarkTickCompleted(tickId: 15, updatedAtUtc: updatedAtUtc);

        Assert.Equal(15, state.LastCompletedTickId);
        Assert.Equal(updatedAtUtc, state.UpdatedAtUtc);
    }

    [Fact]
    public void MarkTickCompleted_WhenTickMovesBackwards_ThrowsInvalidOperationException()
    {
        CityEconomyProgressionState state = CityEconomyProgressionState.Create(
            cityId: Guid.NewGuid(),
            lastCompletedTickId: 12,
            lastProcessedDate: new DateOnly(2048, 2, 3),
            updatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => state.MarkTickCompleted(
                tickId: 11,
                updatedAtUtc: new DateTimeOffset(2048, 2, 3, 8, 0, 0, TimeSpan.Zero)));

        Assert.Equal("Economy progression tick cannot move backwards.", exception.Message);
    }
}
