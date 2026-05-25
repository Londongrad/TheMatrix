using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityResidentialBuildings
{
    public sealed class GetCityResidentialBuildingsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WithoutDistrictFilter_ReturnsMappedBuildings()
        {
            var cityId = new CityId(Guid.NewGuid());
            District district = TopologyTestSupport.CreateDistrict(cityId);
            ResidentialBuilding building = TopologyTestSupport.CreateResidentialBuilding(
                cityId: cityId,
                districtId: district.Id,
                name: "Skyline Tower");
            var repository = new TopologyTestSupport.FakeResidentialBuildingRepository
            {
                Buildings = [building]
            };
            var handler = new GetCityResidentialBuildingsQueryHandler(repository);

            IReadOnlyList<ResidentialBuildingDto> result = await handler.Handle(
                request: new GetCityResidentialBuildingsQuery(
                    CityId: cityId.Value,
                    DistrictId: null),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId.Value,
                actual: repository.RequestedCityId!.Value.Value);
            Assert.Null(repository.RequestedDistrictId);
            ResidentialBuildingDto item = Assert.Single(result);
            Assert.Equal(
                expected: building.Id.Value,
                actual: item.ResidentialBuildingId);
            Assert.Equal(
                expected: building.CityId.Value,
                actual: item.CityId);
            Assert.Equal(
                expected: building.DistrictId.Value,
                actual: item.DistrictId);
            Assert.Equal(
                expected: building.AccessRoadNodeId.Value,
                actual: item.AccessRoadNodeId);
            Assert.Equal(
                expected: "Skyline Tower",
                actual: item.Name);
            Assert.Equal(
                expected: building.Type.ToString(),
                actual: item.Type);
            Assert.Equal(
                expected: building.ResidentCapacity.Value,
                actual: item.ResidentCapacity);
        }

        [Fact]
        public async Task Handle_WithDistrictFilter_PassesDistrictIdToRepository()
        {
            var cityId = new CityId(Guid.NewGuid());
            District district = TopologyTestSupport.CreateDistrict(
                cityId: cityId,
                name: "North");
            var repository = new TopologyTestSupport.FakeResidentialBuildingRepository();
            var handler = new GetCityResidentialBuildingsQueryHandler(repository);

            IReadOnlyList<ResidentialBuildingDto> result = await handler.Handle(
                request: new GetCityResidentialBuildingsQuery(
                    CityId: cityId.Value,
                    DistrictId: district.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.Empty(result);
            Assert.Equal(
                expected: cityId.Value,
                actual: repository.RequestedCityId!.Value.Value);
            Assert.Equal(
                expected: district.Id.Value,
                actual: repository.RequestedDistrictId!.Value.Value);
        }
    }
}
