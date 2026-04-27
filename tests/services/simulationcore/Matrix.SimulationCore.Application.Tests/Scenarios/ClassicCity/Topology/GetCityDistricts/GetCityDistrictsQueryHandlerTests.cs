using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityDistricts;

public sealed class GetCityDistrictsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedDistrictsForRequestedCity()
    {
        var cityId = new CityId(Guid.NewGuid());
        var district = TopologyTestSupport.CreateDistrict(cityId, "Harbor");
        var repository = new TopologyTestSupport.FakeDistrictRepository
        {
            Districts = [district]
        };
        var handler = new GetCityDistrictsQueryHandler(repository);

        var result = await handler.Handle(new GetCityDistrictsQuery(cityId.Value), CancellationToken.None);

        Assert.Equal(cityId.Value, repository.RequestedCityId!.Value.Value);
        var item = Assert.Single(result);
        Assert.Equal(district.Id.Value, item.DistrictId);
        Assert.Equal(district.CityId.Value, item.CityId);
        Assert.Equal("Harbor", item.Name);
        Assert.Equal(district.AnchorX, item.AnchorX);
        Assert.Equal(district.AnchorY, item.AnchorY);
        Assert.Equal(district.CreatedAtUtc, item.CreatedAtUtc);
    }
}
