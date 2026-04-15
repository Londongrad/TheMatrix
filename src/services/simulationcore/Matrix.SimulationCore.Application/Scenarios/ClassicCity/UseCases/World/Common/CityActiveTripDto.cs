namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common
{
    public sealed record CityActiveTripDto(
        Guid TripId,
        Guid CityId,
        Guid? TravellerEntityId,
        string Subject,
        string Purpose,
        string Profile,
        string Status,
        decimal MovementCapabilityIndex,
        bool UsedDynamicRoadConditions,
        long PlannedAtTickId,
        long? ConditionsEffectiveTickId,
        long LastAdvancedTickId,
        DateTimeOffset StartedAtSimTimeUtc,
        DateTimeOffset LastAdvancedAtSimTimeUtc,
        DateTimeOffset ExpectedArrivalAtSimTimeUtc,
        DateTimeOffset? ArrivedAtSimTimeUtc,
        decimal CurrentProgressIndex,
        decimal TotalDistanceMeters,
        decimal DistanceTravelledMeters,
        decimal RemainingDistanceMeters,
        decimal PlannedTravelTimeMinutes,
        decimal AdjustedTravelTimeMinutes,
        CityActiveTripEndpointDto From,
        CityActiveTripEndpointDto To,
        CityActiveTripProgressDto Current);
}
