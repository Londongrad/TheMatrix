using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityAnchors;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityAnchors;

public sealed class GetCityAnchorsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedAnchorsForRequestedCity()
    {
        var cityId = new Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.CityId(Guid.NewGuid());
        var district = TopologyTestSupport.CreateDistrict(cityId);
        var anchor = TopologyTestSupport.CreateCityAnchor(cityId, district.Id, "City Hospital");
        var repository = new TopologyTestSupport.FakeCityAnchorRepository
        {
            Anchors = [anchor]
        };
        var handler = new GetCityAnchorsQueryHandler(repository);

        var result = await handler.Handle(new GetCityAnchorsQuery(cityId.Value), CancellationToken.None);

        Assert.Equal(cityId.Value, repository.RequestedCityId!.Value.Value);
        var item = Assert.Single(result);
        Assert.Equal(anchor.Id.Value, item.CityAnchorId);
        Assert.Equal(anchor.CityId.Value, item.CityId);
        Assert.Equal(anchor.DistrictId.Value, item.DistrictId);
        Assert.Equal(anchor.AccessRoadNodeId.Value, item.AccessRoadNodeId);
        Assert.Equal(anchor.Name.Value, item.Name);
        Assert.Equal(anchor.Type.ToString(), item.Type);
        Assert.Equal(anchor.Capacity, item.Capacity);
    }
}
