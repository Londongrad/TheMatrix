namespace Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population
{
    public sealed record ClassicCityCostOfLivingSnapshotV1(
        Guid CityId,
        decimal WageMultiplier,
        decimal RetailPriceMultiplier,
        decimal HousingCostMultiplier,
        decimal UtilityCostMultiplier,
        decimal CostOfLivingIndex,
        decimal AffordabilityIndex,
        DateTimeOffset OccurredAtUtc);
}
