using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed class CityResourceSupplySnapshot
    {
        public CityResourceSupplySnapshot(
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
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc)
        {
            SupplyStressIndex = NormalizeIndex(
                value: supplyStressIndex,
                paramName: nameof(supplyStressIndex));
            FuelStockLevelIndex = NormalizeIndex(
                value: fuelStockLevelIndex,
                paramName: nameof(fuelStockLevelIndex));
            FuelResupplyReadinessIndex = NormalizeIndex(
                value: fuelResupplyReadinessIndex,
                paramName: nameof(fuelResupplyReadinessIndex));
            FuelShortageRiskIndex = NormalizeIndex(
                value: fuelShortageRiskIndex,
                paramName: nameof(fuelShortageRiskIndex));
            SparePartsStockLevelIndex = NormalizeIndex(
                value: sparePartsStockLevelIndex,
                paramName: nameof(sparePartsStockLevelIndex));
            SparePartsResupplyReadinessIndex = NormalizeIndex(
                value: sparePartsResupplyReadinessIndex,
                paramName: nameof(sparePartsResupplyReadinessIndex));
            SparePartsShortageRiskIndex = NormalizeIndex(
                value: sparePartsShortageRiskIndex,
                paramName: nameof(sparePartsShortageRiskIndex));
            FiltersStockLevelIndex = NormalizeIndex(
                value: filtersStockLevelIndex,
                paramName: nameof(filtersStockLevelIndex));
            FiltersResupplyReadinessIndex = NormalizeIndex(
                value: filtersResupplyReadinessIndex,
                paramName: nameof(filtersResupplyReadinessIndex));
            FiltersShortageRiskIndex = NormalizeIndex(
                value: filtersShortageRiskIndex,
                paramName: nameof(filtersShortageRiskIndex));
            EmergencyWaterStockLevelIndex = NormalizeIndex(
                value: emergencyWaterStockLevelIndex,
                paramName: nameof(emergencyWaterStockLevelIndex));
            EmergencyWaterResupplyReadinessIndex = NormalizeIndex(
                value: emergencyWaterResupplyReadinessIndex,
                paramName: nameof(emergencyWaterResupplyReadinessIndex));
            EmergencyWaterShortageRiskIndex = NormalizeIndex(
                value: emergencyWaterShortageRiskIndex,
                paramName: nameof(emergencyWaterShortageRiskIndex));
            EffectiveTickId = EnsureTickId(
                value: effectiveTickId,
                paramName: nameof(effectiveTickId));
            EffectiveAtUtc = EnsureUtc(
                value: effectiveAtUtc,
                paramName: nameof(effectiveAtUtc));
        }

        public decimal SupplyStressIndex { get; }
        public decimal FuelStockLevelIndex { get; }
        public decimal FuelResupplyReadinessIndex { get; }
        public decimal FuelShortageRiskIndex { get; }
        public decimal SparePartsStockLevelIndex { get; }
        public decimal SparePartsResupplyReadinessIndex { get; }
        public decimal SparePartsShortageRiskIndex { get; }
        public decimal FiltersStockLevelIndex { get; }
        public decimal FiltersResupplyReadinessIndex { get; }
        public decimal FiltersShortageRiskIndex { get; }
        public decimal EmergencyWaterStockLevelIndex { get; }
        public decimal EmergencyWaterResupplyReadinessIndex { get; }
        public decimal EmergencyWaterShortageRiskIndex { get; }
        public long EffectiveTickId { get; }
        public DateTimeOffset EffectiveAtUtc { get; }

        public static CityResourceSupplySnapshot Neutral(
            DateTimeOffset effectiveAtUtc,
            long effectiveTickId = 0)
        {
            return new CityResourceSupplySnapshot(
                supplyStressIndex: 0m,
                fuelStockLevelIndex: 1m,
                fuelResupplyReadinessIndex: 1m,
                fuelShortageRiskIndex: 0m,
                sparePartsStockLevelIndex: 1m,
                sparePartsResupplyReadinessIndex: 1m,
                sparePartsShortageRiskIndex: 0m,
                filtersStockLevelIndex: 1m,
                filtersResupplyReadinessIndex: 1m,
                filtersShortageRiskIndex: 0m,
                emergencyWaterStockLevelIndex: 1m,
                emergencyWaterResupplyReadinessIndex: 1m,
                emergencyWaterShortageRiskIndex: 0m,
                effectiveTickId: effectiveTickId,
                effectiveAtUtc: effectiveAtUtc);
        }

        private static decimal NormalizeIndex(
            decimal value,
            string paramName)
        {
            return decimal.Round(
                d: GuardHelper.AgainstOutOfRange(
                    value: value,
                    min: 0m,
                    max: 1m,
                    errorFactory: ClassicCityDomainErrorsFactory.CityNormalizedIndexOutOfRange,
                    propertyName: paramName),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static DateTimeOffset EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : throw ClassicCityDomainErrorsFactory.CityEnvironmentalTimestampMustBeUtc(
                    value: value,
                    propertyName: paramName);
        }

        private static long EnsureTickId(
            long value,
            string paramName)
        {
            return value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: paramName,
                    message: "Tick identifiers cannot be negative.");
        }
    }
}
