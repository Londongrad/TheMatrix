namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views
{
    public sealed record CityDistrictSanitationConditionsView(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal SanitationSupportIndex,
        IReadOnlyList<CityDistrictSanitationConditionView> Districts);
}
