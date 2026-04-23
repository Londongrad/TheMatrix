using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities;

public sealed class CityUtcOffsetTests
{
    private const string OutOfRangeErrorCode = "SimulationCore.City.UtcOffset.OutOfRange";
    private const string InvalidStepErrorCode = "SimulationCore.City.UtcOffset.InvalidStep";

    [Fact]
    public void Constructor_AcceptsBoundaryValues()
    {
        var min = new CityUtcOffset(TimeSpan.FromMinutes(CityUtcOffset.MinMinutes));
        var max = new CityUtcOffset(TimeSpan.FromMinutes(CityUtcOffset.MaxMinutes));

        Assert.Equal(CityUtcOffset.MinMinutes, min.TotalMinutes);
        Assert.Equal(CityUtcOffset.MaxMinutes, max.TotalMinutes);
    }

    [Fact]
    public void FromMinutes_CreatesOffset()
    {
        var offset = CityUtcOffset.FromMinutes(330);

        Assert.Equal(TimeSpan.FromMinutes(330), offset.Value);
        Assert.Equal(330, offset.TotalMinutes);
    }

    [Fact]
    public void Constructor_WhenMinutesDoNotAlignToStep_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new CityUtcOffset(TimeSpan.FromMinutes(10)));

        Assert.Equal(InvalidStepErrorCode, exception.Code);
    }

    [Fact]
    public void Constructor_WhenValueIsBelowMinimum_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new CityUtcOffset(TimeSpan.FromMinutes(CityUtcOffset.MinMinutes - CityUtcOffset.StepMinutes)));

        Assert.Equal(OutOfRangeErrorCode, exception.Code);
    }

    [Fact]
    public void Constructor_WhenValueIsAboveMaximum_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new CityUtcOffset(TimeSpan.FromMinutes(CityUtcOffset.MaxMinutes + CityUtcOffset.StepMinutes)));

        Assert.Equal(OutOfRangeErrorCode, exception.Code);
    }

    [Fact]
    public void ToString_FormatsPositiveAndNegativeOffsets()
    {
        var positive = CityUtcOffset.FromMinutes(330);
        var negative = CityUtcOffset.FromMinutes(-180);

        Assert.Equal("+05:30", positive.ToString());
        Assert.Equal("-03:00", negative.ToString());
    }
}
