using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology;

public sealed class DistrictTests
{
    [Fact]
    public void Create_WithValidValues_SetsProperties_AndNormalizesCoordinates()
    {
        var district = TopologyTestData.CreateDistrict();

        Assert.Equal(TopologyTestData.CityId, district.CityId);
        Assert.Equal(new DistrictName("Downtown"), district.Name);
        Assert.Equal(12.346m, district.AnchorX);
        Assert.Equal(45.678m, district.AnchorY);
        Assert.Equal(TopologyTestData.CreatedAtUtc, district.CreatedAtUtc);
        Assert.Empty(district.DomainEvents);
    }

    [Fact]
    public void Create_WithNonUtcTimestamp_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => District.Create(
            cityId: TopologyTestData.CityId,
            name: new DistrictName("Downtown"),
            anchorX: 10m,
            anchorY: 20m,
            createdAtUtc: TopologyTestData.NonUtcCreatedAt));

        Assert.Equal("SimulationCore.Topology.Timestamp.NotUtc", exception.Code);
        Assert.Equal("value", exception.PropertyName);
    }

    [Fact]
    public void Rename_WhenNameChanges_UpdatesName()
    {
        var district = TopologyTestData.CreateDistrict();

        district.Rename(new DistrictName("Riverside"));

        Assert.Equal(new DistrictName("Riverside"), district.Name);
    }

    [Fact]
    public void Rename_WithSameName_IsNoOp()
    {
        var district = TopologyTestData.CreateDistrict();

        district.Rename(new DistrictName("Downtown"));

        Assert.Equal(new DistrictName("Downtown"), district.Name);
    }
}
