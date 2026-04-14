namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views
{
    public sealed record CityDistrictUtilityIncidentConditionsView(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal UtilityIncidentSupportIndex,
        IReadOnlyList<CityDistrictUtilityIncidentConditionView> Districts);
}
