namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles
{
    public sealed record SeedCityStockpilesResult(
        SeedCityStockpilesStatus Status,
        Guid CityId,
        decimal SupplyStressIndex,
        bool EmergencyRationingEnabled);
}
