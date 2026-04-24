using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology;

public sealed class ResidentialBuildingNameTests
{
    [Fact]
    public void Constructor_TrimsAndStoresValue()
    {
        var name = new ResidentialBuildingName("  Tower A  ");

        Assert.Equal("Tower A", name.Value);
        Assert.Equal("Tower A", name.ToString());
    }

    [Fact]
    public void Constructor_WhenValueIsNull_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new ResidentialBuildingName(null));

        Assert.Equal("SimulationCore.Topology.ResidentialBuilding.Name.NullOrEmpty", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }

    [Fact]
    public void Constructor_WhenValueIsWhitespace_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new ResidentialBuildingName("   "));

        Assert.Equal("SimulationCore.Topology.ResidentialBuilding.Name.NullOrEmpty", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }

    [Fact]
    public void Constructor_WhenValueIsTooLong_ThrowsDomainException()
    {
        var tooLong = new string('b', ResidentialBuildingName.MaxLength + 1);

        var exception = Assert.Throws<DomainException>(() => new ResidentialBuildingName(tooLong));

        Assert.Equal("SimulationCore.Topology.ResidentialBuilding.Name.TooLong", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }
}
