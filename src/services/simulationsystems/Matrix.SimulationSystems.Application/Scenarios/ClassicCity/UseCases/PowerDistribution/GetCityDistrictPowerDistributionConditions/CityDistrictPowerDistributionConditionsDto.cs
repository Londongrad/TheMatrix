namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityDistrictPowerDistributionConditions
{
    public sealed record CityDistrictPowerDistributionConditionsDto(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal PowerSupportIndex,
        IReadOnlyList<CityDistrictPowerDistributionConditionDto> Districts);
}
