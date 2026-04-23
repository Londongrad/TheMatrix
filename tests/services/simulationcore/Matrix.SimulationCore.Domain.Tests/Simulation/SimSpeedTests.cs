using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation;

public sealed class SimSpeedTests
{
    private const string MultiplierOutOfRangeErrorCode = "SimulationCore.SimSpeed.Multiplier.OutOfRange";
    private const string RealDeltaNotPositiveErrorCode = "SimulationCore.SimSpeed.RealDelta.NotPositive";

    [Fact]
    public void RealTime_ReturnsMultiplierOfOne()
    {
        var speed = SimSpeed.RealTime();

        Assert.Equal(1.0m, speed.Multiplier);
    }

    [Fact]
    public void From_AcceptsBoundaryValues()
    {
        var min = SimSpeed.From(SimSpeed.Min);
        var max = SimSpeed.From(SimSpeed.Max);

        Assert.Equal(SimSpeed.Min, min.Multiplier);
        Assert.Equal(SimSpeed.Max, max.Multiplier);
    }

    [Fact]
    public void From_WhenBelowMin_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => SimSpeed.From(SimSpeed.Min - 0.0001m));

        Assert.Equal(MultiplierOutOfRangeErrorCode, exception.Code);
    }

    [Fact]
    public void From_WhenAboveMax_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => SimSpeed.From(SimSpeed.Max + 0.0001m));

        Assert.Equal(MultiplierOutOfRangeErrorCode, exception.Code);
    }

    [Fact]
    public void Apply_ScalesTime_UsingTickRoundingAwayFromZero()
    {
        var scaled = SimSpeed.From(1.5m).Apply(TimeSpan.FromTicks(1));

        Assert.Equal(TimeSpan.FromTicks(2), scaled);
    }

    [Fact]
    public void Apply_WithZeroDelta_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => SimSpeed.RealTime().Apply(TimeSpan.Zero));

        Assert.Equal(RealDeltaNotPositiveErrorCode, exception.Code);
    }

    [Fact]
    public void Apply_WithNegativeDelta_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => SimSpeed.RealTime().Apply(TimeSpan.FromSeconds(-1)));

        Assert.Equal(RealDeltaNotPositiveErrorCode, exception.Code);
    }
}
