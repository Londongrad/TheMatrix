using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World;

internal static class WorldTestSupport
{
    internal static readonly DateTimeOffset StartedAtUtc = new(2048, 5, 6, 7, 8, 9, TimeSpan.Zero);

    internal static CityActiveTrip CreateActiveTrip(CityId? cityId = null, string subject = "Morning commute")
    {
        CityId actualCityId = cityId ?? new CityId(Guid.NewGuid());
        DistrictId districtId = new(Guid.NewGuid());
        RoadNodeId fromRoadNodeId = RoadNodeId.New();
        RoadNodeId toRoadNodeId = RoadNodeId.New();
        RoadSegmentId roadSegmentId = RoadSegmentId.New();
        IReadOnlyCollection<CityActiveTripSegment> segments =
        [
            CityActiveTripSegment.Create(
                sequence: 0,
                roadSegmentId: roadSegmentId,
                districtId: districtId,
                fromRoadNodeId: fromRoadNodeId,
                toRoadNodeId: toRoadNodeId,
                name: "Downtown Connector",
                type: "Collector",
                lengthMeters: 320m,
                estimatedTraversalMinutes: 8m,
                fromPositionX: 10m,
                fromPositionY: 20m,
                toPositionX: 40m,
                toPositionY: 60m)
        ];

        return CityActiveTrip.Create(
            cityId: actualCityId,
            travellerEntityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            subject: subject,
            purpose: CityTripPurpose.WorkCommute,
            profile: "Pedestrian",
            movementCapabilityIndex: 1m,
            usedDynamicRoadConditions: true,
            plannedAtTickId: 24,
            conditionsEffectiveTickId: 21,
            startedAtSimTimeUtc: StartedAtUtc,
            fromKind: "ResidentialBuilding",
            fromEntityId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            fromDistrictId: districtId,
            fromRoadNodeId: fromRoadNodeId,
            fromName: "River Tower",
            fromPositionX: 10m,
            fromPositionY: 20m,
            toKind: "CityAnchor",
            toEntityId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            toDistrictId: districtId,
            toRoadNodeId: toRoadNodeId,
            toName: "Central Hospital",
            toPositionX: 40m,
            toPositionY: 60m,
            totalDistanceMeters: 320m,
            plannedTravelTimeMinutes: 8m,
            segments: segments);
    }

    internal sealed class FakeCityActiveTripRepository : ICityActiveTripRepository
    {
        public IReadOnlyList<CityActiveTrip> Trips { get; set; } = Array.Empty<CityActiveTrip>();
        public CityId? RequestedCityId { get; private set; }

        public Task AddAsync(CityActiveTrip trip, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<CityActiveTrip>> ListActiveForUpdateByCityIdAsync(CityId cityId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<CityActiveTrip>> ListActiveByCityIdAsync(CityId cityId, CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            return Task.FromResult(Trips);
        }
    }
}
