using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common
{
    internal static class CityActiveTripMappings
    {
        public static CityActiveTripDto ToDto(CityActiveTrip trip)
        {
            return new CityActiveTripDto(
                TripId: trip.Id.Value,
                CityId: trip.CityId.Value,
                TravellerEntityId: trip.TravellerEntityId,
                Subject: trip.Subject,
                Purpose: CityTripPurposeNames.FromDomain(trip.Purpose),
                Profile: trip.Profile,
                Status: ToStatusName(trip.Status),
                MovementCapabilityIndex: trip.MovementCapabilityIndex,
                UsedDynamicRoadConditions: trip.UsedDynamicRoadConditions,
                PlannedAtTickId: trip.PlannedAtTickId,
                ConditionsEffectiveTickId: trip.ConditionsEffectiveTickId,
                LastAdvancedTickId: trip.LastAdvancedTickId,
                StartedAtSimTimeUtc: trip.StartedAtSimTimeUtc,
                LastAdvancedAtSimTimeUtc: trip.LastAdvancedAtSimTimeUtc,
                ExpectedArrivalAtSimTimeUtc: trip.ExpectedArrivalAtSimTimeUtc,
                ArrivedAtSimTimeUtc: trip.ArrivedAtSimTimeUtc,
                CurrentProgressIndex: trip.ProgressIndex,
                TotalDistanceMeters: trip.TotalDistanceMeters,
                DistanceTravelledMeters: trip.DistanceTravelledMeters,
                RemainingDistanceMeters: trip.RemainingDistanceMeters,
                PlannedTravelTimeMinutes: trip.PlannedTravelTimeMinutes,
                AdjustedTravelTimeMinutes: trip.AdjustedTravelTimeMinutes,
                From: new CityActiveTripEndpointDto(
                    Kind: trip.FromKind,
                    EntityId: trip.FromEntityId,
                    DistrictId: trip.FromDistrictId.Value,
                    RoadNodeId: trip.FromRoadNodeId.Value,
                    Name: trip.FromName,
                    PositionX: trip.FromPositionX,
                    PositionY: trip.FromPositionY),
                To: new CityActiveTripEndpointDto(
                    Kind: trip.ToKind,
                    EntityId: trip.ToEntityId,
                    DistrictId: trip.ToDistrictId.Value,
                    RoadNodeId: trip.ToRoadNodeId.Value,
                    Name: trip.ToName,
                    PositionX: trip.ToPositionX,
                    PositionY: trip.ToPositionY),
                Current: new CityActiveTripProgressDto(
                    DistrictId: trip.CurrentDistrictId.Value,
                    RoadSegmentId: trip.CurrentRoadSegmentId?.Value,
                    SegmentProgressIndex: trip.CurrentSegmentProgressIndex,
                    PositionX: trip.CurrentPositionX,
                    PositionY: trip.CurrentPositionY));
        }

        private static string ToStatusName(CityActiveTripStatus value)
        {
            return value switch
            {
                CityActiveTripStatus.Arrived => "Arrived",
                CityActiveTripStatus.Interrupted => "Interrupted",
                _ => "Active"
            };
        }
    }
}
