using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    GetCityEnvironmentalConditions
{
    public sealed record CityResourceSupplyConditionDto(
        decimal SupplyStressIndex,
        DateTimeOffset EffectiveAtUtc,
        CityResourceSupplyLineConditionDto Fuel,
        CityResourceSupplyLineConditionDto SpareParts,
        CityResourceSupplyLineConditionDto Filters,
        CityResourceSupplyLineConditionDto EmergencyWater)
    {
        public static CityResourceSupplyConditionDto FromState(CityResourceSupplyState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            return new CityResourceSupplyConditionDto(
                SupplyStressIndex: state.SupplyStressIndex,
                EffectiveAtUtc: state.EffectiveAtUtc,
                Fuel: new CityResourceSupplyLineConditionDto(
                    StockLevelIndex: state.FuelStockLevelIndex,
                    ResupplyReadinessIndex: state.FuelResupplyReadinessIndex,
                    ShortageRiskIndex: state.FuelShortageRiskIndex),
                SpareParts: new CityResourceSupplyLineConditionDto(
                    StockLevelIndex: state.SparePartsStockLevelIndex,
                    ResupplyReadinessIndex: state.SparePartsResupplyReadinessIndex,
                    ShortageRiskIndex: state.SparePartsShortageRiskIndex),
                Filters: new CityResourceSupplyLineConditionDto(
                    StockLevelIndex: state.FiltersStockLevelIndex,
                    ResupplyReadinessIndex: state.FiltersResupplyReadinessIndex,
                    ShortageRiskIndex: state.FiltersShortageRiskIndex),
                EmergencyWater: new CityResourceSupplyLineConditionDto(
                    StockLevelIndex: state.EmergencyWaterStockLevelIndex,
                    ResupplyReadinessIndex: state.EmergencyWaterResupplyReadinessIndex,
                    ShortageRiskIndex: state.EmergencyWaterShortageRiskIndex));
        }
    }
}
