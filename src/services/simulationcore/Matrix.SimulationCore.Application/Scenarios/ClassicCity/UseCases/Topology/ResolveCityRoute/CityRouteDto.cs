namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    public sealed record CityRouteDto(
        Guid CityId,
        string Profile,
        bool Accessible,
        bool UsedDynamicRoadConditions,
        long? EffectiveTickId,
        DateTimeOffset? ConditionsLastEvaluatedAtUtc,
        CityRoutePointDto From,
        CityRoutePointDto To,
        decimal TotalDistanceMeters,
        decimal EstimatedTravelTimeMinutes,
        decimal OverallPassabilityIndex,
        string? UnreachableReason,
        IReadOnlyList<CityRouteSegmentDto> Segments);
}
