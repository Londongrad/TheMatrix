using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply
{
    public sealed record SyncCityResourceSupplyCommand(
        Guid CityId,
        decimal SupplyStressIndex,
        decimal FuelStockLevelIndex,
        decimal FuelResupplyReadinessIndex,
        decimal FuelShortageRiskIndex,
        decimal SparePartsStockLevelIndex,
        decimal SparePartsResupplyReadinessIndex,
        decimal SparePartsShortageRiskIndex,
        decimal FiltersStockLevelIndex,
        decimal FiltersResupplyReadinessIndex,
        decimal FiltersShortageRiskIndex,
        decimal EmergencyWaterStockLevelIndex,
        decimal EmergencyWaterResupplyReadinessIndex,
        decimal EmergencyWaterShortageRiskIndex,
        long EffectiveTickId,
        DateTimeOffset EffectiveAtUtc)
        : IRequest<SyncCityResourceSupplyResult>;
}
