using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    internal static class TopologyTestData
    {
        internal static readonly CityId CityId = new(Guid.Parse("20000000-0000-0000-0000-000000000001"));
        internal static readonly DistrictId DistrictId = new(Guid.Parse("20000000-0000-0000-0000-000000000002"));
        internal static readonly RoadNodeId RoadNodeId = new(Guid.Parse("20000000-0000-0000-0000-000000000003"));

        internal static readonly RoadNodeId AlternativeRoadNodeId =
            new(Guid.Parse("20000000-0000-0000-0000-000000000004"));

        internal static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2044,
            month: 6,
            day: 7,
            hour: 8,
            minute: 9,
            second: 10,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset NonUtcCreatedAt = new(
            year: 2044,
            month: 6,
            day: 7,
            hour: 8,
            minute: 9,
            second: 10,
            offset: TimeSpan.FromHours(3));

        internal static District CreateDistrict()
        {
            return District.Create(
                cityId: CityId,
                name: new DistrictName("Downtown"),
                anchorX: 12.3456m,
                anchorY: 45.6784m,
                createdAtUtc: CreatedAtUtc);
        }

        internal static CityAnchor CreateCityAnchor()
        {
            return CityAnchor.Create(
                cityId: CityId,
                districtId: DistrictId,
                accessRoadNodeId: RoadNodeId,
                name: new CityAnchorName("Central Hospital"),
                type: CityAnchorType.Hospital,
                capacity: 1200,
                positionX: 22.3456m,
                positionY: 55.4321m,
                createdAtUtc: CreatedAtUtc);
        }

        internal static ResidentialBuilding CreateResidentialBuilding()
        {
            return ResidentialBuilding.Create(
                cityId: CityId,
                districtId: DistrictId,
                accessRoadNodeId: RoadNodeId,
                name: new ResidentialBuildingName("Tower A"),
                type: ResidentialBuildingType.Tower,
                residentCapacity: ResidentCapacity.From(380),
                positionX: 40.1256m,
                positionY: 60.4444m,
                createdAtUtc: CreatedAtUtc);
        }

        internal static RoadNode CreateRoadNode()
        {
            return RoadNode.Create(
                cityId: CityId,
                districtId: DistrictId,
                name: "  North Junction  ",
                type: RoadNodeType.Junction,
                positionX: 18.7654m,
                positionY: 72.1116m,
                createdAtUtc: CreatedAtUtc);
        }

        internal static RoadSegment CreateRoadSegment()
        {
            return RoadSegment.Create(
                cityId: CityId,
                districtId: DistrictId,
                fromRoadNodeId: RoadNodeId,
                toRoadNodeId: AlternativeRoadNodeId,
                name: "  Main Artery  ",
                type: RoadSegmentType.Arterial,
                lengthMeters: 154.555m,
                createdAtUtc: CreatedAtUtc);
        }
    }
}
