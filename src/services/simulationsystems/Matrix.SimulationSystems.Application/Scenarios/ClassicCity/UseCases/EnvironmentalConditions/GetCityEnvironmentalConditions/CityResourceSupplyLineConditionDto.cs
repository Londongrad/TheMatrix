namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions
{
    public sealed record CityResourceSupplyLineConditionDto(
        decimal StockLevelIndex,
        decimal ResupplyReadinessIndex,
        decimal ShortageRiskIndex);
}
