using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityAnchors;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityAnchors
{
    public sealed class GetCityAnchorsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsMappedAnchorsForRequestedCity()
        {
            var cityId = new CityId(Guid.NewGuid());
            District district = TopologyTestSupport.CreateDistrict(cityId);
            CityAnchor anchor = TopologyTestSupport.CreateCityAnchor(
                cityId: cityId,
                districtId: district.Id,
                name: "City Hospital");
            var repository = new TopologyTestSupport.FakeCityAnchorRepository
            {
                Anchors = [anchor]
            };
            var handler = new GetCityAnchorsQueryHandler(repository);

            IReadOnlyList<CityAnchorDto> result = await handler.Handle(
                request: new GetCityAnchorsQuery(cityId.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId.Value,
                actual: repository.RequestedCityId!.Value.Value);
            CityAnchorDto item = Assert.Single(result);
            Assert.Equal(
                expected: anchor.Id.Value,
                actual: item.CityAnchorId);
            Assert.Equal(
                expected: anchor.CityId.Value,
                actual: item.CityId);
            Assert.Equal(
                expected: anchor.DistrictId.Value,
                actual: item.DistrictId);
            Assert.Equal(
                expected: anchor.AccessRoadNodeId.Value,
                actual: item.AccessRoadNodeId);
            Assert.Equal(
                expected: anchor.Name.Value,
                actual: item.Name);
            Assert.Equal(
                expected: anchor.Type.ToString(),
                actual: item.Type);
            Assert.Equal(
                expected: anchor.Capacity,
                actual: item.Capacity);
        }
    }
}
