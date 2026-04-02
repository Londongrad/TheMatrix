namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions
{
    public sealed record CityRoadSegmentConditionsDto(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal RoadSupportIndex,
        IReadOnlyList<CityRoadSegmentConditionDto> Segments);
}
