using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityResourceSupplyState
    {
        private CityResourceSupplyState() { }

        private CityResourceSupplyState(
            decimal supplyStressIndex,
            decimal fuelStockLevelIndex,
            decimal fuelResupplyReadinessIndex,
            decimal fuelShortageRiskIndex,
            decimal sparePartsStockLevelIndex,
            decimal sparePartsResupplyReadinessIndex,
            decimal sparePartsShortageRiskIndex,
            decimal filtersStockLevelIndex,
            decimal filtersResupplyReadinessIndex,
            decimal filtersShortageRiskIndex,
            decimal emergencyWaterStockLevelIndex,
            decimal emergencyWaterResupplyReadinessIndex,
            decimal emergencyWaterShortageRiskIndex,
            DateTimeOffset effectiveAtUtc)
        {
            SupplyStressIndex = supplyStressIndex;
            FuelStockLevelIndex = fuelStockLevelIndex;
            FuelResupplyReadinessIndex = fuelResupplyReadinessIndex;
            FuelShortageRiskIndex = fuelShortageRiskIndex;
            SparePartsStockLevelIndex = sparePartsStockLevelIndex;
            SparePartsResupplyReadinessIndex = sparePartsResupplyReadinessIndex;
            SparePartsShortageRiskIndex = sparePartsShortageRiskIndex;
            FiltersStockLevelIndex = filtersStockLevelIndex;
            FiltersResupplyReadinessIndex = filtersResupplyReadinessIndex;
            FiltersShortageRiskIndex = filtersShortageRiskIndex;
            EmergencyWaterStockLevelIndex = emergencyWaterStockLevelIndex;
            EmergencyWaterResupplyReadinessIndex = emergencyWaterResupplyReadinessIndex;
            EmergencyWaterShortageRiskIndex = emergencyWaterShortageRiskIndex;
            EffectiveAtUtc = effectiveAtUtc;
        }

        public decimal SupplyStressIndex { get; private set; }
        public decimal FuelStockLevelIndex { get; private set; }
        public decimal FuelResupplyReadinessIndex { get; private set; }
        public decimal FuelShortageRiskIndex { get; private set; }
        public decimal SparePartsStockLevelIndex { get; private set; }
        public decimal SparePartsResupplyReadinessIndex { get; private set; }
        public decimal SparePartsShortageRiskIndex { get; private set; }
        public decimal FiltersStockLevelIndex { get; private set; }
        public decimal FiltersResupplyReadinessIndex { get; private set; }
        public decimal FiltersShortageRiskIndex { get; private set; }
        public decimal EmergencyWaterStockLevelIndex { get; private set; }
        public decimal EmergencyWaterResupplyReadinessIndex { get; private set; }
        public decimal EmergencyWaterShortageRiskIndex { get; private set; }
        public DateTimeOffset EffectiveAtUtc { get; private set; }

        public static CityResourceSupplyState Create(CityResourceSupplySnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CityResourceSupplyState(
                supplyStressIndex: snapshot.SupplyStressIndex,
                fuelStockLevelIndex: snapshot.FuelStockLevelIndex,
                fuelResupplyReadinessIndex: snapshot.FuelResupplyReadinessIndex,
                fuelShortageRiskIndex: snapshot.FuelShortageRiskIndex,
                sparePartsStockLevelIndex: snapshot.SparePartsStockLevelIndex,
                sparePartsResupplyReadinessIndex: snapshot.SparePartsResupplyReadinessIndex,
                sparePartsShortageRiskIndex: snapshot.SparePartsShortageRiskIndex,
                filtersStockLevelIndex: snapshot.FiltersStockLevelIndex,
                filtersResupplyReadinessIndex: snapshot.FiltersResupplyReadinessIndex,
                filtersShortageRiskIndex: snapshot.FiltersShortageRiskIndex,
                emergencyWaterStockLevelIndex: snapshot.EmergencyWaterStockLevelIndex,
                emergencyWaterResupplyReadinessIndex: snapshot.EmergencyWaterResupplyReadinessIndex,
                emergencyWaterShortageRiskIndex: snapshot.EmergencyWaterShortageRiskIndex,
                effectiveAtUtc: snapshot.EffectiveAtUtc);
        }

        public void ApplySnapshot(CityResourceSupplySnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            SupplyStressIndex = snapshot.SupplyStressIndex;
            FuelStockLevelIndex = snapshot.FuelStockLevelIndex;
            FuelResupplyReadinessIndex = snapshot.FuelResupplyReadinessIndex;
            FuelShortageRiskIndex = snapshot.FuelShortageRiskIndex;
            SparePartsStockLevelIndex = snapshot.SparePartsStockLevelIndex;
            SparePartsResupplyReadinessIndex = snapshot.SparePartsResupplyReadinessIndex;
            SparePartsShortageRiskIndex = snapshot.SparePartsShortageRiskIndex;
            FiltersStockLevelIndex = snapshot.FiltersStockLevelIndex;
            FiltersResupplyReadinessIndex = snapshot.FiltersResupplyReadinessIndex;
            FiltersShortageRiskIndex = snapshot.FiltersShortageRiskIndex;
            EmergencyWaterStockLevelIndex = snapshot.EmergencyWaterStockLevelIndex;
            EmergencyWaterResupplyReadinessIndex = snapshot.EmergencyWaterResupplyReadinessIndex;
            EmergencyWaterShortageRiskIndex = snapshot.EmergencyWaterShortageRiskIndex;
            EffectiveAtUtc = snapshot.EffectiveAtUtc;
        }

        public CityResourceSupplySnapshot ToSnapshot()
        {
            return new CityResourceSupplySnapshot(
                supplyStressIndex: SupplyStressIndex,
                fuelStockLevelIndex: FuelStockLevelIndex,
                fuelResupplyReadinessIndex: FuelResupplyReadinessIndex,
                fuelShortageRiskIndex: FuelShortageRiskIndex,
                sparePartsStockLevelIndex: SparePartsStockLevelIndex,
                sparePartsResupplyReadinessIndex: SparePartsResupplyReadinessIndex,
                sparePartsShortageRiskIndex: SparePartsShortageRiskIndex,
                filtersStockLevelIndex: FiltersStockLevelIndex,
                filtersResupplyReadinessIndex: FiltersResupplyReadinessIndex,
                filtersShortageRiskIndex: FiltersShortageRiskIndex,
                emergencyWaterStockLevelIndex: EmergencyWaterStockLevelIndex,
                emergencyWaterResupplyReadinessIndex: EmergencyWaterResupplyReadinessIndex,
                emergencyWaterShortageRiskIndex: EmergencyWaterShortageRiskIndex,
                effectiveAtUtc: EffectiveAtUtc);
        }
    }
}
