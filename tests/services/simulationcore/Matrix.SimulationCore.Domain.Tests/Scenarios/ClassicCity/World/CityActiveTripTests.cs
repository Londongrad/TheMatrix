using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.World
{
    public sealed class CityActiveTripTests
    {
        [Fact]
        public void Create_WithValidValues_SetsInitialState_AndOrdersSegments()
        {
            CityActiveTrip trip = WorldTestData.CreateTrip(
                segments:
                [
                    WorldTestData.CreateSecondSegment(),
                    WorldTestData.CreateFirstSegment()
                ]);

            Assert.Equal(
                expected: WorldTestData.CityId,
                actual: trip.CityId);
            Assert.Equal(
                expected: WorldTestData.TravellerEntityId,
                actual: trip.TravellerEntityId);
            Assert.Equal(
                expected: "Resident commute",
                actual: trip.Subject);
            Assert.Equal(
                expected: CityTripPurpose.WorkCommute,
                actual: trip.Purpose);
            Assert.Equal(
                expected: "pedestrian",
                actual: trip.Profile);
            Assert.Equal(
                expected: 1m,
                actual: trip.MovementCapabilityIndex);
            Assert.True(trip.UsedDynamicRoadConditions);
            Assert.Equal(
                expected: 42,
                actual: trip.PlannedAtTickId);
            Assert.Equal(
                expected: 40,
                actual: trip.ConditionsEffectiveTickId);
            Assert.Equal(
                expected: WorldTestData.StartedAtUtc,
                actual: trip.StartedAtSimTimeUtc);
            Assert.Equal(
                expected: WorldTestData.StartedAtUtc,
                actual: trip.LastAdvancedAtSimTimeUtc);
            Assert.Equal(
                expected: 42,
                actual: trip.LastAdvancedTickId);
            Assert.Equal(
                expected: 200m,
                actual: trip.TotalDistanceMeters);
            Assert.Equal(
                expected: 12m,
                actual: trip.PlannedTravelTimeMinutes);
            Assert.Equal(
                expected: 11.32m,
                actual: trip.AdjustedTravelTimeMinutes);
            Assert.Equal(
                expected: 0m,
                actual: trip.ProgressIndex);
            Assert.Equal(
                expected: 0m,
                actual: trip.DistanceTravelledMeters);
            Assert.Equal(
                expected: 200m,
                actual: trip.RemainingDistanceMeters);
            Assert.Equal(
                expected: "district",
                actual: trip.FromKind);
            Assert.Equal(
                expected: WorldTestData.FromEntityId,
                actual: trip.FromEntityId);
            Assert.Equal(
                expected: WorldTestData.FromDistrictId,
                actual: trip.FromDistrictId);
            Assert.Equal(
                expected: WorldTestData.FromRoadNodeId,
                actual: trip.FromRoadNodeId);
            Assert.Equal(
                expected: "Downtown",
                actual: trip.FromName);
            Assert.Equal(
                expected: 10.111m,
                actual: trip.FromPositionX);
            Assert.Equal(
                expected: 20.222m,
                actual: trip.FromPositionY);
            Assert.Equal(
                expected: "anchor",
                actual: trip.ToKind);
            Assert.Equal(
                expected: WorldTestData.ToEntityId,
                actual: trip.ToEntityId);
            Assert.Equal(
                expected: WorldTestData.ToDistrictId,
                actual: trip.ToDistrictId);
            Assert.Equal(
                expected: WorldTestData.ToRoadNodeId,
                actual: trip.ToRoadNodeId);
            Assert.Equal(
                expected: "Office Campus",
                actual: trip.ToName);
            Assert.Equal(
                expected: 70.778m,
                actual: trip.ToPositionX);
            Assert.Equal(
                expected: 80.889m,
                actual: trip.ToPositionY);
            Assert.Equal(
                expected: WorldTestData.FromDistrictId,
                actual: trip.CurrentDistrictId);
            Assert.Equal(
                expected: WorldTestData.FirstRoadSegmentId,
                actual: trip.CurrentRoadSegmentId);
            Assert.Equal(
                expected: 0m,
                actual: trip.CurrentSegmentProgressIndex);
            Assert.Equal(
                expected: 10.111m,
                actual: trip.CurrentPositionX);
            Assert.Equal(
                expected: 20.222m,
                actual: trip.CurrentPositionY);
            Assert.Equal(
                expected: CityActiveTripStatus.Active,
                actual: trip.Status);
            Assert.True(trip.IsActive);
            Assert.Null(trip.ArrivedAtSimTimeUtc);
            Assert.Collection(
                collection: trip.Segments,
                segment => Assert.Equal(
                    expected: 0,
                    actual: segment.Sequence),
                segment => Assert.Equal(
                    expected: 1,
                    actual: segment.Sequence));
            Assert.Empty(trip.DomainEvents);
        }

        [Fact]
        public void Create_WithNoSegments_ArrivesImmediately()
        {
            CityActiveTrip trip = WorldTestData.CreateTrip(
                segments: [],
                totalDistanceMeters: 200m,
                plannedTravelTimeMinutes: 12m);

            Assert.Equal(
                expected: CityActiveTripStatus.Arrived,
                actual: trip.Status);
            Assert.False(trip.IsActive);
            Assert.Equal(
                expected: WorldTestData.StartedAtUtc,
                actual: trip.ArrivedAtSimTimeUtc);
            Assert.Equal(
                expected: 1m,
                actual: trip.ProgressIndex);
            Assert.Equal(
                expected: 1m,
                actual: trip.CurrentSegmentProgressIndex);
            Assert.Equal(
                expected: 200m,
                actual: trip.DistanceTravelledMeters);
            Assert.Equal(
                expected: 0m,
                actual: trip.RemainingDistanceMeters);
            Assert.Equal(
                expected: WorldTestData.ToDistrictId,
                actual: trip.CurrentDistrictId);
            Assert.Null(trip.CurrentRoadSegmentId);
            Assert.Equal(
                expected: 70.778m,
                actual: trip.CurrentPositionX);
            Assert.Equal(
                expected: 80.889m,
                actual: trip.CurrentPositionY);
        }

        [Fact]
        public void Create_WithInvalidMovementCapability_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => WorldTestData.CreateTrip(
                movementCapabilityIndex: CityActiveTrip.MovementCapabilityIndexMin - 0.01m));

            Assert.Equal(
                expected: "SimulationCore.World.ActiveTrip.Capability.OutOfRange",
                actual: exception.Code);
            Assert.Equal(
                expected: "MovementCapabilityIndex",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithNonUtcTimestamp_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => WorldTestData.CreateTrip(
                startedAtSimTimeUtc: WorldTestData.NonUtcStartedAt));

            Assert.Equal(
                expected: "SimulationCore.World.ActiveTrip.Timestamp.NotUtc",
                actual: exception.Code);
            Assert.Equal(
                expected: "value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithInvalidPurpose_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityActiveTrip.Create(
                cityId: WorldTestData.CityId,
                travellerEntityId: WorldTestData.TravellerEntityId,
                subject: "Resident commute",
                purpose: (CityTripPurpose)999,
                profile: "pedestrian",
                movementCapabilityIndex: 1m,
                usedDynamicRoadConditions: true,
                plannedAtTickId: 42,
                conditionsEffectiveTickId: 40,
                startedAtSimTimeUtc: WorldTestData.StartedAtUtc,
                fromKind: "district",
                fromEntityId: WorldTestData.FromEntityId,
                fromDistrictId: WorldTestData.FromDistrictId,
                fromRoadNodeId: WorldTestData.FromRoadNodeId,
                fromName: "Downtown",
                fromPositionX: 10m,
                fromPositionY: 20m,
                toKind: "anchor",
                toEntityId: WorldTestData.ToEntityId,
                toDistrictId: WorldTestData.ToDistrictId,
                toRoadNodeId: WorldTestData.ToRoadNodeId,
                toName: "Office Campus",
                toPositionX: 70m,
                toPositionY: 80m,
                totalDistanceMeters: 200m,
                plannedTravelTimeMinutes: 12m,
                segments: WorldTestData.CreateSegments()));

            Assert.Equal(
                expected: "Domain.Guard.InvalidEnum",
                actual: exception.Code);
            Assert.Equal(
                expected: "Purpose",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithTooLongSubject_ThrowsDomainException()
        {
            string tooLong = new(
                c: 's',
                count: CityActiveTrip.MaxSubjectLength + 1);

            DomainException exception = Assert.Throws<DomainException>(() => CityActiveTrip.Create(
                cityId: WorldTestData.CityId,
                travellerEntityId: WorldTestData.TravellerEntityId,
                subject: tooLong,
                purpose: CityTripPurpose.WorkCommute,
                profile: "pedestrian",
                movementCapabilityIndex: 1m,
                usedDynamicRoadConditions: true,
                plannedAtTickId: 42,
                conditionsEffectiveTickId: 40,
                startedAtSimTimeUtc: WorldTestData.StartedAtUtc,
                fromKind: "district",
                fromEntityId: WorldTestData.FromEntityId,
                fromDistrictId: WorldTestData.FromDistrictId,
                fromRoadNodeId: WorldTestData.FromRoadNodeId,
                fromName: "Downtown",
                fromPositionX: 10m,
                fromPositionY: 20m,
                toKind: "anchor",
                toEntityId: WorldTestData.ToEntityId,
                toDistrictId: WorldTestData.ToDistrictId,
                toRoadNodeId: WorldTestData.ToRoadNodeId,
                toName: "Office Campus",
                toPositionX: 70m,
                toPositionY: 80m,
                totalDistanceMeters: 200m,
                plannedTravelTimeMinutes: 12m,
                segments: WorldTestData.CreateSegments()));

            Assert.Equal(
                expected: "SimulationCore.World.ActiveTrip.Subject.TooLong",
                actual: exception.Code);
            Assert.Equal(
                expected: "Subject",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithWhitespaceProfile_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityActiveTrip.Create(
                cityId: WorldTestData.CityId,
                travellerEntityId: WorldTestData.TravellerEntityId,
                subject: "Resident commute",
                purpose: CityTripPurpose.WorkCommute,
                profile: "   ",
                movementCapabilityIndex: 1m,
                usedDynamicRoadConditions: true,
                plannedAtTickId: 42,
                conditionsEffectiveTickId: 40,
                startedAtSimTimeUtc: WorldTestData.StartedAtUtc,
                fromKind: "district",
                fromEntityId: WorldTestData.FromEntityId,
                fromDistrictId: WorldTestData.FromDistrictId,
                fromRoadNodeId: WorldTestData.FromRoadNodeId,
                fromName: "Downtown",
                fromPositionX: 10m,
                fromPositionY: 20m,
                toKind: "anchor",
                toEntityId: WorldTestData.ToEntityId,
                toDistrictId: WorldTestData.ToDistrictId,
                toRoadNodeId: WorldTestData.ToRoadNodeId,
                toName: "Office Campus",
                toPositionX: 70m,
                toPositionY: 80m,
                totalDistanceMeters: 200m,
                plannedTravelTimeMinutes: 12m,
                segments: WorldTestData.CreateSegments()));

            Assert.Equal(
                expected: "SimulationCore.World.ActiveTrip.Profile.NullOrEmpty",
                actual: exception.Code);
            Assert.Equal(
                expected: "Profile",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithZeroDistance_ArrivesImmediatelyEvenWithSegments()
        {
            CityActiveTrip trip = WorldTestData.CreateTrip(
                totalDistanceMeters: 0m,
                plannedTravelTimeMinutes: 12m);

            Assert.Equal(
                expected: CityActiveTripStatus.Arrived,
                actual: trip.Status);
            Assert.False(trip.IsActive);
            Assert.Equal(
                expected: WorldTestData.StartedAtUtc,
                actual: trip.ArrivedAtSimTimeUtc);
            Assert.Equal(
                expected: 1m,
                actual: trip.ProgressIndex);
            Assert.Equal(
                expected: 0m,
                actual: trip.DistanceTravelledMeters);
            Assert.Equal(
                expected: 0m,
                actual: trip.RemainingDistanceMeters);
            Assert.Equal(
                expected: WorldTestData.ToDistrictId,
                actual: trip.CurrentDistrictId);
            Assert.Null(trip.CurrentRoadSegmentId);
            Assert.Equal(
                expected: 70.778m,
                actual: trip.CurrentPositionX);
            Assert.Equal(
                expected: 80.889m,
                actual: trip.CurrentPositionY);
        }

        [Fact]
        public void Create_WithNullTravellerEntityId_AllowsAnonymousTrip()
        {
            var trip = CityActiveTrip.Create(
                cityId: WorldTestData.CityId,
                travellerEntityId: null,
                subject: "Visitor route",
                purpose: CityTripPurpose.LeisureWalk,
                profile: "visitor",
                movementCapabilityIndex: 0.9m,
                usedDynamicRoadConditions: false,
                plannedAtTickId: 50,
                conditionsEffectiveTickId: null,
                startedAtSimTimeUtc: WorldTestData.StartedAtUtc,
                fromKind: "district",
                fromEntityId: WorldTestData.FromEntityId,
                fromDistrictId: WorldTestData.FromDistrictId,
                fromRoadNodeId: WorldTestData.FromRoadNodeId,
                fromName: "Downtown",
                fromPositionX: 10m,
                fromPositionY: 20m,
                toKind: "anchor",
                toEntityId: WorldTestData.ToEntityId,
                toDistrictId: WorldTestData.ToDistrictId,
                toRoadNodeId: WorldTestData.ToRoadNodeId,
                toName: "Park",
                toPositionX: 70m,
                toPositionY: 80m,
                totalDistanceMeters: 200m,
                plannedTravelTimeMinutes: 15m,
                segments: WorldTestData.CreateSegments());

            Assert.Null(trip.TravellerEntityId);
            Assert.Equal(
                expected: "Visitor route",
                actual: trip.Subject);
            Assert.Equal(
                expected: CityTripPurpose.LeisureWalk,
                actual: trip.Purpose);
            Assert.Equal(
                expected: "visitor",
                actual: trip.Profile);
            Assert.Null(trip.ConditionsEffectiveTickId);
        }

        [Fact]
        public void AdvanceTo_WhenTimeMovesForward_UpdatesProgressDistance_AndCurrentSegmentState()
        {
            CityActiveTrip trip = WorldTestData.CreateTrip();

            trip.AdvanceTo(
                toSimTimeUtc: WorldTestData.StartedAtUtc.AddMinutes(3),
                tickId: 43);

            Assert.Equal(
                expected: WorldTestData.StartedAtUtc.AddMinutes(3),
                actual: trip.LastAdvancedAtSimTimeUtc);
            Assert.Equal(
                expected: 43,
                actual: trip.LastAdvancedTickId);
            Assert.Equal(
                expected: 0.2650m,
                actual: trip.ProgressIndex);
            Assert.Equal(
                expected: 53m,
                actual: trip.DistanceTravelledMeters);
            Assert.Equal(
                expected: 147m,
                actual: trip.RemainingDistanceMeters);
            Assert.Equal(
                expected: WorldTestData.FromDistrictId,
                actual: trip.CurrentDistrictId);
            Assert.Equal(
                expected: WorldTestData.FirstRoadSegmentId,
                actual: trip.CurrentRoadSegmentId);
            Assert.Equal(
                expected: 0.4417m,
                actual: trip.CurrentSegmentProgressIndex);
            Assert.Equal(
                expected: 19.0424m,
                actual: trip.CurrentPositionX);
            Assert.Equal(
                expected: 29.1534m,
                actual: trip.CurrentPositionY);
            Assert.Equal(
                expected: CityActiveTripStatus.Active,
                actual: trip.Status);
            Assert.Null(trip.ArrivedAtSimTimeUtc);
        }

        [Fact]
        public void AdvanceTo_WhenTripArrives_SetsArrivalState_AndFinalPosition()
        {
            CityActiveTrip trip = WorldTestData.CreateTrip();

            trip.AdvanceTo(
                toSimTimeUtc: WorldTestData.StartedAtUtc.AddMinutes(20),
                tickId: 44);

            Assert.Equal(
                expected: CityActiveTripStatus.Arrived,
                actual: trip.Status);
            Assert.False(trip.IsActive);
            Assert.Equal(
                expected: WorldTestData.StartedAtUtc.AddMinutes(20),
                actual: trip.ArrivedAtSimTimeUtc);
            Assert.Equal(
                expected: 1m,
                actual: trip.ProgressIndex);
            Assert.Equal(
                expected: 200m,
                actual: trip.DistanceTravelledMeters);
            Assert.Equal(
                expected: 0m,
                actual: trip.RemainingDistanceMeters);
            Assert.Equal(
                expected: WorldTestData.ToDistrictId,
                actual: trip.CurrentDistrictId);
            Assert.Null(trip.CurrentRoadSegmentId);
            Assert.Equal(
                expected: 1m,
                actual: trip.CurrentSegmentProgressIndex);
            Assert.Equal(
                expected: 70.778m,
                actual: trip.CurrentPositionX);
            Assert.Equal(
                expected: 80.889m,
                actual: trip.CurrentPositionY);
        }

        [Fact]
        public void AdvanceTo_WhenTripMovesIntoSecondSegment_SwitchesSegmentContext()
        {
            CityActiveTrip trip = WorldTestData.CreateTrip();

            trip.AdvanceTo(
                toSimTimeUtc: WorldTestData.StartedAtUtc.AddMinutes(8),
                tickId: 44);

            Assert.Equal(
                expected: WorldTestData.ToDistrictId,
                actual: trip.CurrentDistrictId);
            Assert.Equal(
                expected: WorldTestData.SecondRoadSegmentId,
                actual: trip.CurrentRoadSegmentId);
            Assert.InRange(
                actual: trip.CurrentSegmentProgressIndex,
                low: 0.2667m,
                high: 0.2668m);
            Assert.InRange(
                actual: trip.CurrentPositionX,
                low: 30.3333m,
                high: 70.7777m);
            Assert.InRange(
                actual: trip.CurrentPositionY,
                low: 40.4444m,
                high: 80.8888m);
            Assert.Equal(
                expected: CityActiveTripStatus.Active,
                actual: trip.Status);
            Assert.Null(trip.ArrivedAtSimTimeUtc);
        }

        [Fact]
        public void AdvanceTo_WhenTimeDoesNotMoveForward_IsNoOp()
        {
            CityActiveTrip trip = WorldTestData.CreateTrip();

            trip.AdvanceTo(
                toSimTimeUtc: WorldTestData.StartedAtUtc,
                tickId: 99);

            Assert.Equal(
                expected: WorldTestData.StartedAtUtc,
                actual: trip.LastAdvancedAtSimTimeUtc);
            Assert.Equal(
                expected: 42,
                actual: trip.LastAdvancedTickId);
            Assert.Equal(
                expected: 0m,
                actual: trip.ProgressIndex);
            Assert.Equal(
                expected: 0m,
                actual: trip.DistanceTravelledMeters);
            Assert.Equal(
                expected: WorldTestData.FirstRoadSegmentId,
                actual: trip.CurrentRoadSegmentId);
            Assert.Equal(
                expected: CityActiveTripStatus.Active,
                actual: trip.Status);
        }

        [Fact]
        public void AdvanceTo_WithNonUtcTimestamp_ThrowsDomainException()
        {
            CityActiveTrip trip = WorldTestData.CreateTrip();

            DomainException exception = Assert.Throws<DomainException>(() => trip.AdvanceTo(
                toSimTimeUtc: WorldTestData.NonUtcStartedAt,
                tickId: 43));

            Assert.Equal(
                expected: "SimulationCore.World.ActiveTrip.Timestamp.NotUtc",
                actual: exception.Code);
            Assert.Equal(
                expected: "value",
                actual: exception.PropertyName);
        }

        [Fact]
        public void AdvanceTo_WhenTripIsAlreadyArrived_IsNoOp()
        {
            CityActiveTrip trip = WorldTestData.CreateTrip(segments: []);

            trip.AdvanceTo(
                toSimTimeUtc: WorldTestData.StartedAtUtc.AddMinutes(5),
                tickId: 45);

            Assert.Equal(
                expected: WorldTestData.StartedAtUtc,
                actual: trip.LastAdvancedAtSimTimeUtc);
            Assert.Equal(
                expected: 42,
                actual: trip.LastAdvancedTickId);
            Assert.Equal(
                expected: WorldTestData.StartedAtUtc,
                actual: trip.ArrivedAtSimTimeUtc);
            Assert.Equal(
                expected: 1m,
                actual: trip.ProgressIndex);
            Assert.Equal(
                expected: 200m,
                actual: trip.DistanceTravelledMeters);
            Assert.Equal(
                expected: CityActiveTripStatus.Arrived,
                actual: trip.Status);
        }
    }
}
