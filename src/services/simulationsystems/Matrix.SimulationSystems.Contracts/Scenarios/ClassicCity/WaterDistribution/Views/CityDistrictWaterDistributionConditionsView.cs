namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views
{
    public sealed record CityDistrictWaterDistributionConditionsView(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal WaterSupportIndex,
        IReadOnlyList<CityDistrictWaterDistributionConditionView> Districts);
}
