using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology;

public sealed class CityAnchorNameTests
{
    [Fact]
    public void Constructor_TrimsAndStoresValue()
    {
        var name = new CityAnchorName("  Central Station  ");

        Assert.Equal("Central Station", name.Value);
        Assert.Equal("Central Station", name.ToString());
    }

    [Fact]
    public void Constructor_WhenValueIsNull_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new CityAnchorName(null));

        Assert.Equal("SimulationCore.Topology.CityAnchor.Name.NullOrEmpty", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }

    [Fact]
    public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new CityAnchorName("   "));

        Assert.Equal("SimulationCore.Topology.CityAnchor.Name.NullOrEmpty", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }

    [Fact]
    public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
    {
        var tooLong = new string('a', CityAnchorName.MaxLength + 1);

        var exception = Assert.Throws<DomainException>(() => new CityAnchorName(tooLong));

        Assert.Equal("SimulationCore.Topology.CityAnchor.Name.TooLong", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }
}
