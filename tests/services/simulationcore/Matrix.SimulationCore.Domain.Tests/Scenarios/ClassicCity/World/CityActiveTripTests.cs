using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.World;

public sealed class CityActiveTripTests
{
    [Fact]
    public void Create_WithValidValues_SetsInitialState_AndOrdersSegments()
    {
        var trip = WorldTestData.CreateTrip(
            segments:
            [
                WorldTestData.CreateSecondSegment(),
                WorldTestData.CreateFirstSegment()
            ]);

        Assert.Equal(WorldTestData.CityId, trip.CityId);
        Assert.Equal(WorldTestData.TravellerEntityId, trip.TravellerEntityId);
        Assert.Equal("Resident commute", trip.Subject);
        Assert.Equal(CityTripPurpose.WorkCommute, trip.Purpose);
        Assert.Equal("pedestrian", trip.Profile);
        Assert.Equal(1m, trip.MovementCapabilityIndex);
        Assert.True(trip.UsedDynamicRoadConditions);
        Assert.Equal(42, trip.PlannedAtTickId);
        Assert.Equal(40, trip.ConditionsEffectiveTickId);
        Assert.Equal(WorldTestData.StartedAtUtc, trip.StartedAtSimTimeUtc);
        Assert.Equal(WorldTestData.StartedAtUtc, trip.LastAdvancedAtSimTimeUtc);
        Assert.Equal(42, trip.LastAdvancedTickId);
        Assert.Equal(200m, trip.TotalDistanceMeters);
        Assert.Equal(12m, trip.PlannedTravelTimeMinutes);
        Assert.Equal(11.32m, trip.AdjustedTravelTimeMinutes);
        Assert.Equal(0m, trip.ProgressIndex);
        Assert.Equal(0m, trip.DistanceTravelledMeters);
        Assert.Equal(200m, trip.RemainingDistanceMeters);
        Assert.Equal("district", trip.FromKind);
        Assert.Equal(WorldTestData.FromEntityId, trip.FromEntityId);
        Assert.Equal(WorldTestData.FromDistrictId, trip.FromDistrictId);
        Assert.Equal(WorldTestData.FromRoadNodeId, trip.FromRoadNodeId);
        Assert.Equal("Downtown", trip.FromName);
        Assert.Equal(10.111m, trip.FromPositionX);
        Assert.Equal(20.222m, trip.FromPositionY);
        Assert.Equal("anchor", trip.ToKind);
        Assert.Equal(WorldTestData.ToEntityId, trip.ToEntityId);
        Assert.Equal(WorldTestData.ToDistrictId, trip.ToDistrictId);
        Assert.Equal(WorldTestData.ToRoadNodeId, trip.ToRoadNodeId);
        Assert.Equal("Office Campus", trip.ToName);
        Assert.Equal(70.778m, trip.ToPositionX);
        Assert.Equal(80.889m, trip.ToPositionY);
        Assert.Equal(WorldTestData.FromDistrictId, trip.CurrentDistrictId);
        Assert.Equal(WorldTestData.FirstRoadSegmentId, trip.CurrentRoadSegmentId);
        Assert.Equal(0m, trip.CurrentSegmentProgressIndex);
        Assert.Equal(10.111m, trip.CurrentPositionX);
        Assert.Equal(20.222m, trip.CurrentPositionY);
        Assert.Equal(CityActiveTripStatus.Active, trip.Status);
        Assert.True(trip.IsActive);
        Assert.Null(trip.ArrivedAtSimTimeUtc);
        Assert.Collection(
            trip.Segments,
            segment => Assert.Equal(0, segment.Sequence),
            segment => Assert.Equal(1, segment.Sequence));
        Assert.Empty(trip.DomainEvents);
    }

    [Fact]
    public void Create_WithNoSegments_ArrivesImmediately()
    {
        var trip = WorldTestData.CreateTrip(
            segments: [],
            totalDistanceMeters: 200m,
            plannedTravelTimeMinutes: 12m);

        Assert.Equal(CityActiveTripStatus.Arrived, trip.Status);
        Assert.False(trip.IsActive);
        Assert.Equal(WorldTestData.StartedAtUtc, trip.ArrivedAtSimTimeUtc);
        Assert.Equal(1m, trip.ProgressIndex);
        Assert.Equal(1m, trip.CurrentSegmentProgressIndex);
        Assert.Equal(200m, trip.DistanceTravelledMeters);
        Assert.Equal(0m, trip.RemainingDistanceMeters);
        Assert.Equal(WorldTestData.ToDistrictId, trip.CurrentDistrictId);
        Assert.Null(trip.CurrentRoadSegmentId);
        Assert.Equal(70.778m, trip.CurrentPositionX);
        Assert.Equal(80.889m, trip.CurrentPositionY);
    }

    [Fact]
    public void Create_WithInvalidMovementCapability_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => WorldTestData.CreateTrip(
            movementCapabilityIndex: CityActiveTrip.MovementCapabilityIndexMin - 0.01m));

        Assert.Equal("SimulationCore.World.ActiveTrip.Capability.OutOfRange", exception.Code);
        Assert.Equal("MovementCapabilityIndex", exception.PropertyName);
    }

    [Fact]
    public void Create_WithNonUtcTimestamp_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => WorldTestData.CreateTrip(
            startedAtSimTimeUtc: WorldTestData.NonUtcStartedAt));

        Assert.Equal("SimulationCore.World.ActiveTrip.Timestamp.NotUtc", exception.Code);
        Assert.Equal("value", exception.PropertyName);
    }

    [Fact]
    public void Create_WithInvalidPurpose_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityActiveTrip.Create(
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

        Assert.Equal("Domain.Guard.InvalidEnum", exception.Code);
        Assert.Equal("Purpose", exception.PropertyName);
    }

    [Fact]
    public void Create_WithTooLongSubject_ThrowsDomainException()
    {
        var tooLong = new string('s', CityActiveTrip.MaxSubjectLength + 1);

        var exception = Assert.Throws<DomainException>(() => CityActiveTrip.Create(
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

        Assert.Equal("SimulationCore.World.ActiveTrip.Subject.TooLong", exception.Code);
        Assert.Equal("Subject", exception.PropertyName);
    }

    [Fact]
    public void Create_WithWhitespaceProfile_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityActiveTrip.Create(
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

        Assert.Equal("SimulationCore.World.ActiveTrip.Profile.NullOrEmpty", exception.Code);
        Assert.Equal("Profile", exception.PropertyName);
    }

    [Fact]
    public void Create_WithZeroDistance_ArrivesImmediatelyEvenWithSegments()
    {
        var trip = WorldTestData.CreateTrip(
            totalDistanceMeters: 0m,
            plannedTravelTimeMinutes: 12m);

        Assert.Equal(CityActiveTripStatus.Arrived, trip.Status);
        Assert.False(trip.IsActive);
        Assert.Equal(WorldTestData.StartedAtUtc, trip.ArrivedAtSimTimeUtc);
        Assert.Equal(1m, trip.ProgressIndex);
        Assert.Equal(0m, trip.DistanceTravelledMeters);
        Assert.Equal(0m, trip.RemainingDistanceMeters);
        Assert.Equal(WorldTestData.ToDistrictId, trip.CurrentDistrictId);
        Assert.Null(trip.CurrentRoadSegmentId);
        Assert.Equal(70.778m, trip.CurrentPositionX);
        Assert.Equal(80.889m, trip.CurrentPositionY);
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
        Assert.Equal("Visitor route", trip.Subject);
        Assert.Equal(CityTripPurpose.LeisureWalk, trip.Purpose);
        Assert.Equal("visitor", trip.Profile);
        Assert.Null(trip.ConditionsEffectiveTickId);
    }

    [Fact]
    public void AdvanceTo_WhenTimeMovesForward_UpdatesProgressDistance_AndCurrentSegmentState()
    {
        var trip = WorldTestData.CreateTrip();

        trip.AdvanceTo(
            toSimTimeUtc: WorldTestData.StartedAtUtc.AddMinutes(3),
            tickId: 43);

        Assert.Equal(WorldTestData.StartedAtUtc.AddMinutes(3), trip.LastAdvancedAtSimTimeUtc);
        Assert.Equal(43, trip.LastAdvancedTickId);
        Assert.Equal(0.2650m, trip.ProgressIndex);
        Assert.Equal(53m, trip.DistanceTravelledMeters);
        Assert.Equal(147m, trip.RemainingDistanceMeters);
        Assert.Equal(WorldTestData.FromDistrictId, trip.CurrentDistrictId);
        Assert.Equal(WorldTestData.FirstRoadSegmentId, trip.CurrentRoadSegmentId);
        Assert.Equal(0.4417m, trip.CurrentSegmentProgressIndex);
        Assert.Equal(19.0424m, trip.CurrentPositionX);
        Assert.Equal(29.1534m, trip.CurrentPositionY);
        Assert.Equal(CityActiveTripStatus.Active, trip.Status);
        Assert.Null(trip.ArrivedAtSimTimeUtc);
    }

    [Fact]
    public void AdvanceTo_WhenTripArrives_SetsArrivalState_AndFinalPosition()
    {
        var trip = WorldTestData.CreateTrip();

        trip.AdvanceTo(
            toSimTimeUtc: WorldTestData.StartedAtUtc.AddMinutes(20),
            tickId: 44);

        Assert.Equal(CityActiveTripStatus.Arrived, trip.Status);
        Assert.False(trip.IsActive);
        Assert.Equal(WorldTestData.StartedAtUtc.AddMinutes(20), trip.ArrivedAtSimTimeUtc);
        Assert.Equal(1m, trip.ProgressIndex);
        Assert.Equal(200m, trip.DistanceTravelledMeters);
        Assert.Equal(0m, trip.RemainingDistanceMeters);
        Assert.Equal(WorldTestData.ToDistrictId, trip.CurrentDistrictId);
        Assert.Null(trip.CurrentRoadSegmentId);
        Assert.Equal(1m, trip.CurrentSegmentProgressIndex);
        Assert.Equal(70.778m, trip.CurrentPositionX);
        Assert.Equal(80.889m, trip.CurrentPositionY);
    }

    [Fact]
    public void AdvanceTo_WhenTripMovesIntoSecondSegment_SwitchesSegmentContext()
    {
        var trip = WorldTestData.CreateTrip();

        trip.AdvanceTo(
            toSimTimeUtc: WorldTestData.StartedAtUtc.AddMinutes(8),
            tickId: 44);

        Assert.Equal(WorldTestData.ToDistrictId, trip.CurrentDistrictId);
        Assert.Equal(WorldTestData.SecondRoadSegmentId, trip.CurrentRoadSegmentId);
        Assert.InRange(trip.CurrentSegmentProgressIndex, 0.2667m, 0.2668m);
        Assert.InRange(trip.CurrentPositionX, 30.3333m, 70.7777m);
        Assert.InRange(trip.CurrentPositionY, 40.4444m, 80.8888m);
        Assert.Equal(CityActiveTripStatus.Active, trip.Status);
        Assert.Null(trip.ArrivedAtSimTimeUtc);
    }

    [Fact]
    public void AdvanceTo_WhenTimeDoesNotMoveForward_IsNoOp()
    {
        var trip = WorldTestData.CreateTrip();

        trip.AdvanceTo(
            toSimTimeUtc: WorldTestData.StartedAtUtc,
            tickId: 99);

        Assert.Equal(WorldTestData.StartedAtUtc, trip.LastAdvancedAtSimTimeUtc);
        Assert.Equal(42, trip.LastAdvancedTickId);
        Assert.Equal(0m, trip.ProgressIndex);
        Assert.Equal(0m, trip.DistanceTravelledMeters);
        Assert.Equal(WorldTestData.FirstRoadSegmentId, trip.CurrentRoadSegmentId);
        Assert.Equal(CityActiveTripStatus.Active, trip.Status);
    }

    [Fact]
    public void AdvanceTo_WithNonUtcTimestamp_ThrowsDomainException()
    {
        var trip = WorldTestData.CreateTrip();

        var exception = Assert.Throws<DomainException>(() => trip.AdvanceTo(
            toSimTimeUtc: WorldTestData.NonUtcStartedAt,
            tickId: 43));

        Assert.Equal("SimulationCore.World.ActiveTrip.Timestamp.NotUtc", exception.Code);
        Assert.Equal("value", exception.PropertyName);
    }

    [Fact]
    public void AdvanceTo_WhenTripIsAlreadyArrived_IsNoOp()
    {
        var trip = WorldTestData.CreateTrip(segments: []);

        trip.AdvanceTo(
            toSimTimeUtc: WorldTestData.StartedAtUtc.AddMinutes(5),
            tickId: 45);

        Assert.Equal(WorldTestData.StartedAtUtc, trip.LastAdvancedAtSimTimeUtc);
        Assert.Equal(42, trip.LastAdvancedTickId);
        Assert.Equal(WorldTestData.StartedAtUtc, trip.ArrivedAtSimTimeUtc);
        Assert.Equal(1m, trip.ProgressIndex);
        Assert.Equal(200m, trip.DistanceTravelledMeters);
        Assert.Equal(CityActiveTripStatus.Arrived, trip.Status);
    }
}
