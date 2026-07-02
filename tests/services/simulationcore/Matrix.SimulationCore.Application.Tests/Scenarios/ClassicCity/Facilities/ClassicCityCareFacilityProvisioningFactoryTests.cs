using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Facilities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Facilities;

public sealed class ClassicCityCareFacilityProvisioningFactoryTests
{
    [Fact]
    public void Create_MapsOnlyHospitalAnchorsToNeutralFacilities()
    {
        City city = ClassicCityTestSupport.CreateCity();
        District district = TopologyTestSupport.CreateDistrict(city.Id);
        CityAnchor hospital = TopologyTestSupport.CreateCityAnchor(
            cityId: city.Id,
            districtId: district.Id,
            name: "Central Hospital");
        CityAnchor workplace = CityAnchor.Create(
            cityId: city.Id,
            districtId: district.Id,
            accessRoadNodeId: RoadNodeId.New(),
            name: new CityAnchorName("Factory"),
            type: CityAnchorType.Workplace,
            capacity: 800,
            positionX: 10m,
            positionY: 20m,
            createdAtUtc: TopologyTestSupport.CreatedAtUtc);

        CareFacilityProvisioningBatch batch =
            ClassicCityCareFacilityProvisioningFactory.Create(city, [workplace, hospital]);

        Assert.Equal(city.Id.Value, batch.SimulationHostId);
        Assert.Equal(ClassicCityCareFacilityProvisioningFactory.InitialSourceRevision, batch.SourceRevision);
        Assert.Equal(city.CreatedAtUtc, batch.SynchronizedAtUtc);
        Assert.Equal($"simulation:{city.Id.Value:N}:care-facilities:0", batch.CorrelationId);
        CareFacilityProvisioning facility = Assert.Single(batch.Facilities);
        Assert.Equal(hospital.Id.Value, facility.FacilityId);
        Assert.Equal(hospital.Id.Value, facility.LocationAnchorId);
        Assert.Equal(hospital.Name.Value, facility.Name);
        Assert.Equal("Hospital", facility.Kind);
        Assert.Equal(hospital.Capacity, facility.DailyPatientCapacity);
        Assert.True(facility.IsActive);
    }
}
