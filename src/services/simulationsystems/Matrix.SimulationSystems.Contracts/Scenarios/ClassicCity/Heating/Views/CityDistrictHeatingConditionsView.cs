namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views
{
    public sealed record CityDistrictHeatingConditionsView(
        Guid CityId,
        long EffectiveTickId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal HeatingSupportIndex,
        IReadOnlyList<CityDistrictHeatingConditionView> Districts);
}
