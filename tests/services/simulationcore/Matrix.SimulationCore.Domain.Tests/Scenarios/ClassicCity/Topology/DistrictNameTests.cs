using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology;

public sealed class DistrictNameTests
{
    [Fact]
    public void Constructor_TrimsAndStoresValue()
    {
        var name = new DistrictName("  Old Town  ");

        Assert.Equal("Old Town", name.Value);
        Assert.Equal("Old Town", name.ToString());
    }

    [Fact]
    public void Constructor_WhenValueIsNull_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new DistrictName(null));

        Assert.Equal("SimulationCore.Topology.District.Name.NullOrEmpty", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }

    [Fact]
    public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new DistrictName("  "));

        Assert.Equal("SimulationCore.Topology.District.Name.NullOrEmpty", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }

    [Fact]
    public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
    {
        var tooLong = new string('d', DistrictName.MaxLength + 1);

        var exception = Assert.Throws<DomainException>(() => new DistrictName(tooLong));

        Assert.Equal("SimulationCore.Topology.District.Name.TooLong", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }
}
