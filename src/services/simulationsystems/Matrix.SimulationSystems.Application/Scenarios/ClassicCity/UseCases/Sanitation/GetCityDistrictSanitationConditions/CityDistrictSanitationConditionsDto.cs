namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.
    GetCityDistrictSanitationConditions
{
    public sealed record CityDistrictSanitationConditionsDto(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal SanitationSupportIndex,
        IReadOnlyList<CityDistrictSanitationConditionDto> Districts);
}
