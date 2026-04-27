using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityResidentialBuildings;

public sealed class GetCityResidentialBuildingsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithoutDistrictFilter_ReturnsMappedBuildings()
    {
        var cityId = new CityId(Guid.NewGuid());
        var district = TopologyTestSupport.CreateDistrict(cityId);
        var building = TopologyTestSupport.CreateResidentialBuilding(cityId, district.Id, "Skyline Tower");
        var repository = new TopologyTestSupport.FakeResidentialBuildingRepository
        {
            Buildings = [building]
        };
        var handler = new GetCityResidentialBuildingsQueryHandler(repository);

        var result = await handler.Handle(new GetCityResidentialBuildingsQuery(cityId.Value, null), CancellationToken.None);

        Assert.Equal(cityId.Value, repository.RequestedCityId!.Value.Value);
        Assert.Null(repository.RequestedDistrictId);
        var item = Assert.Single(result);
        Assert.Equal(building.Id.Value, item.ResidentialBuildingId);
        Assert.Equal(building.CityId.Value, item.CityId);
        Assert.Equal(building.DistrictId.Value, item.DistrictId);
        Assert.Equal(building.AccessRoadNodeId.Value, item.AccessRoadNodeId);
        Assert.Equal("Skyline Tower", item.Name);
        Assert.Equal(building.Type.ToString(), item.Type);
        Assert.Equal(building.ResidentCapacity.Value, item.ResidentCapacity);
    }

    [Fact]
    public async Task Handle_WithDistrictFilter_PassesDistrictIdToRepository()
    {
        var cityId = new CityId(Guid.NewGuid());
        var district = TopologyTestSupport.CreateDistrict(cityId, "North");
        var repository = new TopologyTestSupport.FakeResidentialBuildingRepository();
        var handler = new GetCityResidentialBuildingsQueryHandler(repository);

        var result = await handler.Handle(new GetCityResidentialBuildingsQuery(cityId.Value, district.Id.Value), CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(cityId.Value, repository.RequestedCityId!.Value.Value);
        Assert.Equal(district.Id.Value, repository.RequestedDistrictId!.Value.Value);
    }
}
