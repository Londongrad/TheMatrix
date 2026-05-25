namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    GetCityDistrictWaterDistributionConditions
{
    public sealed record CityDistrictWaterDistributionConditionsDto(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal WaterSupportIndex,
        IReadOnlyList<CityDistrictWaterDistributionConditionDto> Districts);
}
