using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.World
{
    internal static class WorldTestData
    {
        internal static readonly CityId CityId = new(Guid.Parse("30000000-0000-0000-0000-000000000001"));
        internal static readonly DistrictId FromDistrictId = new(Guid.Parse("30000000-0000-0000-0000-000000000002"));
        internal static readonly DistrictId ToDistrictId = new(Guid.Parse("30000000-0000-0000-0000-000000000003"));
        internal static readonly RoadNodeId FromRoadNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000004"));
        internal static readonly RoadNodeId MidRoadNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000005"));
        internal static readonly RoadNodeId ToRoadNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000006"));

        internal static readonly RoadSegmentId FirstRoadSegmentId =
            new(Guid.Parse("30000000-0000-0000-0000-000000000007"));

        internal static readonly RoadSegmentId SecondRoadSegmentId =
            new(Guid.Parse("30000000-0000-0000-0000-000000000008"));

        internal static readonly Guid FromEntityId = Guid.Parse("30000000-0000-0000-0000-000000000009");
        internal static readonly Guid ToEntityId = Guid.Parse("30000000-0000-0000-0000-000000000010");
        internal static readonly Guid TravellerEntityId = Guid.Parse("30000000-0000-0000-0000-000000000011");

        internal static readonly DateTimeOffset StartedAtUtc = new(
            year: 2045,
            month: 7,
            day: 8,
            hour: 9,
            minute: 10,
            second: 11,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset NonUtcStartedAt = new(
            year: 2045,
            month: 7,
            day: 8,
            hour: 9,
            minute: 10,
            second: 11,
            offset: TimeSpan.FromHours(3));

        internal static CityActiveTripSegment CreateFirstSegment()
        {
            return CityActiveTripSegment.Create(
                sequence: 0,
                roadSegmentId: FirstRoadSegmentId,
                districtId: FromDistrictId,
                fromRoadNodeId: FromRoadNodeId,
                toRoadNodeId: MidRoadNodeId,
                name: "  Segment A  ",
                type: "  arterial  ",
                lengthMeters: 120m,
                estimatedTraversalMinutes: 6m,
                fromPositionX: 10.1111m,
                fromPositionY: 20.2222m,
                toPositionX: 30.3333m,
                toPositionY: 40.4444m);
        }

        internal static CityActiveTripSegment CreateSecondSegment()
        {
            return CityActiveTripSegment.Create(
                sequence: 1,
                roadSegmentId: SecondRoadSegmentId,
                districtId: ToDistrictId,
                fromRoadNodeId: MidRoadNodeId,
                toRoadNodeId: ToRoadNodeId,
                name: "Segment B",
                type: "collector",
                lengthMeters: 80m,
                estimatedTraversalMinutes: 4m,
                fromPositionX: 30.3333m,
                fromPositionY: 40.4444m,
                toPositionX: 70.7777m,
                toPositionY: 80.8888m);
        }

        internal static IReadOnlyCollection<CityActiveTripSegment> CreateSegments()
        {
            return
            [
                CreateFirstSegment(),
                CreateSecondSegment()
            ];
        }

        internal static CityActiveTrip CreateTrip(
            IReadOnlyCollection<CityActiveTripSegment>? segments = null,
            decimal movementCapabilityIndex = 1m,
            decimal totalDistanceMeters = 200m,
            decimal plannedTravelTimeMinutes = 12m,
            DateTimeOffset? startedAtSimTimeUtc = null)
        {
            return CityActiveTrip.Create(
                cityId: CityId,
                travellerEntityId: TravellerEntityId,
                subject: "  Resident commute  ",
                purpose: CityTripPurpose.WorkCommute,
                profile: "  pedestrian  ",
                movementCapabilityIndex: movementCapabilityIndex,
                usedDynamicRoadConditions: true,
                plannedAtTickId: 42,
                conditionsEffectiveTickId: 40,
                startedAtSimTimeUtc: startedAtSimTimeUtc ?? StartedAtUtc,
                fromKind: "  district  ",
                fromEntityId: FromEntityId,
                fromDistrictId: FromDistrictId,
                fromRoadNodeId: FromRoadNodeId,
                fromName: "  Downtown  ",
                fromPositionX: 10.1111m,
                fromPositionY: 20.2222m,
                toKind: "  anchor  ",
                toEntityId: ToEntityId,
                toDistrictId: ToDistrictId,
                toRoadNodeId: ToRoadNodeId,
                toName: "  Office Campus  ",
                toPositionX: 70.7777m,
                toPositionY: 80.8888m,
                totalDistanceMeters: totalDistanceMeters,
                plannedTravelTimeMinutes: plannedTravelTimeMinutes,
                segments: segments ?? CreateSegments());
        }
    }
}
