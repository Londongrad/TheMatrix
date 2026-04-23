using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation;

public sealed class SimTimeTests
{
    private const string SimTimeNotUtcErrorCode = "SimulationCore.SimTime.NotUtc";

    [Fact]
    public void FromUtc_AcceptsUtcDateTimeOffset()
    {
        var value = new DateTimeOffset(2035, 6, 7, 8, 9, 10, TimeSpan.Zero);

        var simTime = SimTime.FromUtc(value);

        Assert.Equal(value, simTime.ValueUtc);
    }

    [Fact]
    public void FromUtc_WhenOffsetIsNotZero_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => SimTime.FromUtc(
            new DateTimeOffset(2035, 6, 7, 8, 9, 10, TimeSpan.FromHours(3))));

        Assert.Equal(SimTimeNotUtcErrorCode, exception.Code);
    }

    [Fact]
    public void Add_ShiftsTime_AndPreservesUtcOffset()
    {
        var start = SimTime.FromUtc(new DateTimeOffset(2035, 6, 7, 8, 9, 10, TimeSpan.Zero));

        var result = start.Add(TimeSpan.FromMinutes(90));

        Assert.Equal(new DateTimeOffset(2035, 6, 7, 9, 39, 10, TimeSpan.Zero), result.ValueUtc);
        Assert.Equal(TimeSpan.Zero, result.ValueUtc.Offset);
    }
}
