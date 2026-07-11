using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Institutions;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Institutions;

public sealed class ClassicCityEducationInstitutionProvisioningFactoryTests
{
    [Fact]
    public void Create_MapsOnlySchoolAnchorsToEducationInstitutions()
    {
        City city = ClassicCityTestSupport.CreateCity();
        District district = TopologyTestSupport.CreateDistrict(city.Id);
        CityAnchor school = CreateAnchor(
            city: city,
            district: district,
            name: "Central Education Complex",
            type: CityAnchorType.School,
            capacity: 640);
        CityAnchor workplace = CreateAnchor(
            city: city,
            district: district,
            name: "Factory",
            type: CityAnchorType.Workplace,
            capacity: 800);

        EducationInstitutionProvisioningBatch batch =
            ClassicCityEducationInstitutionProvisioningFactory.Create(city, [workplace, school]);

        Assert.Equal(
            expected: city.Id.Value,
            actual: batch.SimulationHostId);
        Assert.Equal(
            expected: ClassicCityEducationInstitutionProvisioningFactory.InitialSourceRevision,
            actual: batch.SourceRevision);
        Assert.Equal(
            expected: city.CreatedAtUtc,
            actual: batch.SynchronizedAtUtc);
        Assert.Equal(
            expected: $"simulation:{city.Id.Value:N}:education-institutions:0",
            actual: batch.CorrelationId);
        EducationInstitutionProvisioning institution = Assert.Single(batch.Institutions);
        Assert.Equal(
            expected: school.Id.Value,
            actual: institution.InstitutionId);
        Assert.Equal(
            expected: school.Id.Value,
            actual: institution.LocationAnchorId);
        Assert.Equal(
            expected: school.Name.Value,
            actual: institution.Name);
        Assert.Equal(
            expected: "School",
            actual: institution.Kind);
        Assert.Equal(
            expected: school.Capacity,
            actual: institution.Capacity);
        Assert.True(institution.IsActive);
    }

    private static CityAnchor CreateAnchor(
        City city,
        District district,
        string name,
        CityAnchorType type,
        int capacity)
    {
        return CityAnchor.Create(
            cityId: city.Id,
            districtId: district.Id,
            accessRoadNodeId: RoadNodeId.New(),
            name: new CityAnchorName(name),
            type: type,
            capacity: capacity,
            positionX: 10m,
            positionY: 20m,
            createdAtUtc: TopologyTestSupport.CreatedAtUtc);
    }
}
