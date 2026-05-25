using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationLivingConditionsState
    {
        private CityPopulationLivingConditionsState() { }

        private CityPopulationLivingConditionsState(
            CityId cityId,
            decimal floodingIndex,
            decimal roadAccessibilityIndex,
            decimal powerCoverageIndex,
            decimal utilityContinuityIndex,
            decimal heatingCoverageIndex,
            decimal waterCoverageIndex,
            decimal sanitationCoverageIndex,
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            CityId = cityId;
            FloodingIndex = ValidateIndex(
                value: floodingIndex,
                paramName: nameof(floodingIndex));
            RoadAccessibilityIndex = ValidateIndex(
                value: roadAccessibilityIndex,
                paramName: nameof(roadAccessibilityIndex));
            PowerCoverageIndex = ValidateIndex(
                value: powerCoverageIndex,
                paramName: nameof(powerCoverageIndex));
            UtilityContinuityIndex = ValidateIndex(
                value: utilityContinuityIndex,
                paramName: nameof(utilityContinuityIndex));
            HeatingCoverageIndex = ValidateIndex(
                value: heatingCoverageIndex,
                paramName: nameof(heatingCoverageIndex));
            WaterCoverageIndex = ValidateIndex(
                value: waterCoverageIndex,
                paramName: nameof(waterCoverageIndex));
            SanitationCoverageIndex = ValidateIndex(
                value: sanitationCoverageIndex,
                paramName: nameof(sanitationCoverageIndex));
            EffectiveTickId = EnsureTickId(
                value: effectiveTickId,
                paramName: nameof(effectiveTickId));
            EffectiveAtUtc = EnsureUtc(
                value: effectiveAtUtc,
                paramName: nameof(effectiveAtUtc));
            UpdatedAtUtc = EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));
        }

        public CityId CityId { get; private set; }
        public decimal FloodingIndex { get; private set; }
        public decimal RoadAccessibilityIndex { get; private set; }
        public decimal PowerCoverageIndex { get; private set; }
        public decimal UtilityContinuityIndex { get; private set; }
        public decimal HeatingCoverageIndex { get; private set; }
        public decimal WaterCoverageIndex { get; private set; }
        public decimal SanitationCoverageIndex { get; private set; }
        public long EffectiveTickId { get; private set; }
        public DateTimeOffset EffectiveAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityPopulationLivingConditionsState Create(
            CityId cityId,
            decimal floodingIndex,
            decimal roadAccessibilityIndex,
            decimal powerCoverageIndex,
            decimal utilityContinuityIndex,
            decimal heatingCoverageIndex,
            decimal waterCoverageIndex,
            decimal sanitationCoverageIndex,
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationLivingConditionsState(
                cityId: cityId,
                floodingIndex: floodingIndex,
                roadAccessibilityIndex: roadAccessibilityIndex,
                powerCoverageIndex: powerCoverageIndex,
                utilityContinuityIndex: utilityContinuityIndex,
                heatingCoverageIndex: heatingCoverageIndex,
                waterCoverageIndex: waterCoverageIndex,
                sanitationCoverageIndex: sanitationCoverageIndex,
                effectiveTickId: effectiveTickId,
                effectiveAtUtc: effectiveAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void ApplySnapshot(
            decimal floodingIndex,
            decimal roadAccessibilityIndex,
            decimal powerCoverageIndex,
            decimal utilityContinuityIndex,
            decimal heatingCoverageIndex,
            decimal waterCoverageIndex,
            decimal sanitationCoverageIndex,
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            FloodingIndex = ValidateIndex(
                value: floodingIndex,
                paramName: nameof(floodingIndex));
            RoadAccessibilityIndex = ValidateIndex(
                value: roadAccessibilityIndex,
                paramName: nameof(roadAccessibilityIndex));
            PowerCoverageIndex = ValidateIndex(
                value: powerCoverageIndex,
                paramName: nameof(powerCoverageIndex));
            UtilityContinuityIndex = ValidateIndex(
                value: utilityContinuityIndex,
                paramName: nameof(utilityContinuityIndex));
            HeatingCoverageIndex = ValidateIndex(
                value: heatingCoverageIndex,
                paramName: nameof(heatingCoverageIndex));
            WaterCoverageIndex = ValidateIndex(
                value: waterCoverageIndex,
                paramName: nameof(waterCoverageIndex));
            SanitationCoverageIndex = ValidateIndex(
                value: sanitationCoverageIndex,
                paramName: nameof(sanitationCoverageIndex));
            EffectiveTickId = EnsureTickId(
                value: effectiveTickId,
                paramName: nameof(effectiveTickId));
            EffectiveAtUtc = EnsureUtc(
                value: effectiveAtUtc,
                paramName: nameof(effectiveAtUtc));
            UpdatedAtUtc = EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));
        }

        private static decimal ValidateIndex(
            decimal value,
            string paramName)
        {
            if (value is < 0m or > 3m)
                throw new ArgumentOutOfRangeException(paramName);

            return decimal.Round(
                d: value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static long EnsureTickId(
            long value,
            string paramName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName);

            return value;
        }

        private static DateTimeOffset EnsureUtc(
            DateTimeOffset value,
            string paramName)
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
