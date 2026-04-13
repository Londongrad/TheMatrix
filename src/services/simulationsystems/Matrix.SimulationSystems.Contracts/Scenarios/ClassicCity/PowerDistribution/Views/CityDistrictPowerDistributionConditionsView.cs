namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views
{
    public sealed record CityDistrictPowerDistributionConditionsView(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal PowerSupportIndex,
        IReadOnlyList<CityDistrictPowerDistributionConditionView> Districts);
}
