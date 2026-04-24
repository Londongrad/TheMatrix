using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology;

public sealed class ResidentCapacityTests
{
    [Fact]
    public void From_AcceptsBoundaries()
    {
        Assert.Equal(ResidentCapacity.Min, ResidentCapacity.From(ResidentCapacity.Min).Value);
        Assert.Equal(ResidentCapacity.Max, ResidentCapacity.From(ResidentCapacity.Max).Value);
    }

    [Fact]
    public void From_WhenBelowMinimum_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => ResidentCapacity.From(ResidentCapacity.Min - 1));

        Assert.Equal("SimulationCore.Topology.ResidentialBuilding.Capacity.OutOfRange", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }

    [Fact]
    public void From_WhenAboveMaximum_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => ResidentCapacity.From(ResidentCapacity.Max + 1));

        Assert.Equal("SimulationCore.Topology.ResidentialBuilding.Capacity.OutOfRange", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }

    [Fact]
    public void ToString_ReturnsNumericValue()
    {
        var capacity = ResidentCapacity.From(320);

        Assert.Equal("320", capacity.ToString());
    }
}
