namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Views
{
    public sealed record CityRouteView(
        Guid CityId,
        string Profile,
        bool Accessible,
        bool UsedDynamicRoadConditions,
        long? EffectiveTickId,
        DateTimeOffset? ConditionsLastEvaluatedAtUtc,
        CityRoutePointView From,
        CityRoutePointView To,
        decimal TotalDistanceMeters,
        decimal EstimatedTravelTimeMinutes,
        decimal OverallPassabilityIndex,
        string? UnreachableReason,
        IReadOnlyList<CityRouteSegmentView> Segments);
}
