namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions
{
    public sealed record CityDistrictHeatingConditionsDto(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal HeatingSupportIndex,
        IReadOnlyList<CityDistrictHeatingConditionDto> Districts);
}
