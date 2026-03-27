namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views
{
    public sealed record CityResourceSupplyConditionView(
        decimal SupplyStressIndex,
        DateTimeOffset EffectiveAtUtc,
        CityResourceSupplyLineConditionView Fuel,
        CityResourceSupplyLineConditionView SpareParts,
        CityResourceSupplyLineConditionView Filters,
        CityResourceSupplyLineConditionView EmergencyWater);
}
