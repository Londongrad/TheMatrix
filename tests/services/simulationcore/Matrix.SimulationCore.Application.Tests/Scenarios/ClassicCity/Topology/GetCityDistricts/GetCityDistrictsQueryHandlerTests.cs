using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityDistricts
{
    public sealed class GetCityDistrictsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsMappedDistrictsForRequestedCity()
        {
            var cityId = new CityId(Guid.NewGuid());
            District district = TopologyTestSupport.CreateDistrict(
                cityId: cityId,
                name: "Harbor");
            var repository = new TopologyTestSupport.FakeDistrictRepository
            {
                Districts = [district]
            };
            var handler = new GetCityDistrictsQueryHandler(repository);

            IReadOnlyList<DistrictDto> result = await handler.Handle(
                request: new GetCityDistrictsQuery(cityId.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId.Value,
                actual: repository.RequestedCityId!.Value.Value);
            DistrictDto item = Assert.Single(result);
            Assert.Equal(
                expected: district.Id.Value,
                actual: item.DistrictId);
            Assert.Equal(
                expected: district.CityId.Value,
                actual: item.CityId);
            Assert.Equal(
                expected: "Harbor",
                actual: item.Name);
            Assert.Equal(
                expected: district.AnchorX,
                actual: item.AnchorX);
            Assert.Equal(
                expected: district.AnchorY,
                actual: item.AnchorY);
            Assert.Equal(
                expected: district.CreatedAtUtc,
                actual: item.CreatedAtUtc);
        }
    }
}
