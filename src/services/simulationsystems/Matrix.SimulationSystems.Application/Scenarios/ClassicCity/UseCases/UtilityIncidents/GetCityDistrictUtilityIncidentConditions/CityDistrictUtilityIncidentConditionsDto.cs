namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    GetCityDistrictUtilityIncidentConditions
{
    public sealed record CityDistrictUtilityIncidentConditionsDto(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal UtilityIncidentSupportIndex,
        IReadOnlyList<CityDistrictUtilityIncidentConditionDto> Districts);
}
