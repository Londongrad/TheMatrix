using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationEssentialsState
    {
        private CityPopulationEssentialsState() { }

        private CityPopulationEssentialsState(
            CityId cityId,
            decimal supplyStressIndex,
            bool emergencyRationingEnabled,
            decimal foodStockLevelIndex,
            decimal foodShortageRiskIndex,
            decimal medicineStockLevelIndex,
            decimal medicineShortageRiskIndex,
            decimal emergencyWaterStockLevelIndex,
            decimal emergencyWaterShortageRiskIndex,
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            CityId = cityId;
            SupplyStressIndex = ValidateIndex(supplyStressIndex, nameof(supplyStressIndex));
            EmergencyRationingEnabled = emergencyRationingEnabled;
            FoodStockLevelIndex = ValidateIndex(foodStockLevelIndex, nameof(foodStockLevelIndex));
            FoodShortageRiskIndex = ValidateIndex(foodShortageRiskIndex, nameof(foodShortageRiskIndex));
            MedicineStockLevelIndex = ValidateIndex(medicineStockLevelIndex, nameof(medicineStockLevelIndex));
            MedicineShortageRiskIndex = ValidateIndex(medicineShortageRiskIndex, nameof(medicineShortageRiskIndex));
            EmergencyWaterStockLevelIndex = ValidateIndex(emergencyWaterStockLevelIndex, nameof(emergencyWaterStockLevelIndex));
            EmergencyWaterShortageRiskIndex = ValidateIndex(emergencyWaterShortageRiskIndex, nameof(emergencyWaterShortageRiskIndex));
            EffectiveTickId = EnsureTickId(effectiveTickId, nameof(effectiveTickId));
            EffectiveAtUtc = EnsureUtc(effectiveAtUtc, nameof(effectiveAtUtc));
            UpdatedAtUtc = EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        }

        public CityId CityId { get; private set; }
        public decimal SupplyStressIndex { get; private set; }
        public bool EmergencyRationingEnabled { get; private set; }
        public decimal FoodStockLevelIndex { get; private set; }
        public decimal FoodShortageRiskIndex { get; private set; }
        public decimal MedicineStockLevelIndex { get; private set; }
        public decimal MedicineShortageRiskIndex { get; private set; }
        public decimal EmergencyWaterStockLevelIndex { get; private set; }
        public decimal EmergencyWaterShortageRiskIndex { get; private set; }
        public long EffectiveTickId { get; private set; }
        public DateTimeOffset EffectiveAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityPopulationEssentialsState Create(
            CityId cityId,
            decimal supplyStressIndex,
            bool emergencyRationingEnabled,
            decimal foodStockLevelIndex,
            decimal foodShortageRiskIndex,
            decimal medicineStockLevelIndex,
            decimal medicineShortageRiskIndex,
            decimal emergencyWaterStockLevelIndex,
            decimal emergencyWaterShortageRiskIndex,
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationEssentialsState(
                cityId: cityId,
                supplyStressIndex: supplyStressIndex,
                emergencyRationingEnabled: emergencyRationingEnabled,
                foodStockLevelIndex: foodStockLevelIndex,
                foodShortageRiskIndex: foodShortageRiskIndex,
                medicineStockLevelIndex: medicineStockLevelIndex,
                medicineShortageRiskIndex: medicineShortageRiskIndex,
                emergencyWaterStockLevelIndex: emergencyWaterStockLevelIndex,
                emergencyWaterShortageRiskIndex: emergencyWaterShortageRiskIndex,
                effectiveTickId: effectiveTickId,
                effectiveAtUtc: effectiveAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void ApplySnapshot(
            decimal supplyStressIndex,
            bool emergencyRationingEnabled,
            decimal foodStockLevelIndex,
            decimal foodShortageRiskIndex,
            decimal medicineStockLevelIndex,
            decimal medicineShortageRiskIndex,
            decimal emergencyWaterStockLevelIndex,
            decimal emergencyWaterShortageRiskIndex,
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            SupplyStressIndex = ValidateIndex(supplyStressIndex, nameof(supplyStressIndex));
            EmergencyRationingEnabled = emergencyRationingEnabled;
            FoodStockLevelIndex = ValidateIndex(foodStockLevelIndex, nameof(foodStockLevelIndex));
            FoodShortageRiskIndex = ValidateIndex(foodShortageRiskIndex, nameof(foodShortageRiskIndex));
            MedicineStockLevelIndex = ValidateIndex(medicineStockLevelIndex, nameof(medicineStockLevelIndex));
            MedicineShortageRiskIndex = ValidateIndex(medicineShortageRiskIndex, nameof(medicineShortageRiskIndex));
            EmergencyWaterStockLevelIndex = ValidateIndex(emergencyWaterStockLevelIndex, nameof(emergencyWaterStockLevelIndex));
            EmergencyWaterShortageRiskIndex = ValidateIndex(emergencyWaterShortageRiskIndex, nameof(emergencyWaterShortageRiskIndex));
            EffectiveTickId = EnsureTickId(effectiveTickId, nameof(effectiveTickId));
            EffectiveAtUtc = EnsureUtc(effectiveAtUtc, nameof(effectiveAtUtc));
            UpdatedAtUtc = EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        }

        private static decimal ValidateIndex(decimal value, string paramName)
        {
            if (value is < 0m or > 3m)
                throw new ArgumentOutOfRangeException(paramName);

            return decimal.Round(value, 4, MidpointRounding.AwayFromZero);
        }

        private static long EnsureTickId(long value, string paramName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName);

            return value;
        }

        private static DateTimeOffset EnsureUtc(DateTimeOffset value, string paramName)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.TimestampMustBeUtc,
                propertyName: paramName);

            return value;
        }
    }
}
