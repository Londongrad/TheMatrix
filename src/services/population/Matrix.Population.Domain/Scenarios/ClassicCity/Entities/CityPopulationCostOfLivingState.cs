using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationCostOfLivingState
    {
        private CityPopulationCostOfLivingState() { }

        private CityPopulationCostOfLivingState(
            CityId cityId,
            decimal wageMultiplier,
            decimal retailPriceMultiplier,
            decimal housingCostMultiplier,
            decimal utilityCostMultiplier,
            decimal costOfLivingIndex,
            decimal affordabilityIndex,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            CityId = cityId;
            WageMultiplier = ValidateMultiplier(
                value: wageMultiplier,
                paramName: nameof(wageMultiplier));
            RetailPriceMultiplier = ValidateMultiplier(
                value: retailPriceMultiplier,
                paramName: nameof(retailPriceMultiplier));
            HousingCostMultiplier = ValidateMultiplier(
                value: housingCostMultiplier,
                paramName: nameof(housingCostMultiplier));
            UtilityCostMultiplier = ValidateMultiplier(
                value: utilityCostMultiplier,
                paramName: nameof(utilityCostMultiplier));
            CostOfLivingIndex = ValidateIndex(
                value: costOfLivingIndex,
                paramName: nameof(costOfLivingIndex));
            AffordabilityIndex = ValidateIndex(
                value: affordabilityIndex,
                paramName: nameof(affordabilityIndex));
            LastEvaluatedAtUtc = EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
            UpdatedAtUtc = EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));
        }

        public CityId CityId { get; private set; }
        public decimal WageMultiplier { get; private set; }
        public decimal RetailPriceMultiplier { get; private set; }
        public decimal HousingCostMultiplier { get; private set; }
        public decimal UtilityCostMultiplier { get; private set; }
        public decimal CostOfLivingIndex { get; private set; }
        public decimal AffordabilityIndex { get; private set; }
        public DateTimeOffset LastEvaluatedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityPopulationCostOfLivingState Create(
            CityId cityId,
            decimal wageMultiplier,
            decimal retailPriceMultiplier,
            decimal housingCostMultiplier,
            decimal utilityCostMultiplier,
            decimal costOfLivingIndex,
            decimal affordabilityIndex,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationCostOfLivingState(
                cityId: cityId,
                wageMultiplier: wageMultiplier,
                retailPriceMultiplier: retailPriceMultiplier,
                housingCostMultiplier: housingCostMultiplier,
                utilityCostMultiplier: utilityCostMultiplier,
                costOfLivingIndex: costOfLivingIndex,
                affordabilityIndex: affordabilityIndex,
                lastEvaluatedAtUtc: lastEvaluatedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void ApplySnapshot(
            decimal wageMultiplier,
            decimal retailPriceMultiplier,
            decimal housingCostMultiplier,
            decimal utilityCostMultiplier,
            decimal costOfLivingIndex,
            decimal affordabilityIndex,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            WageMultiplier = ValidateMultiplier(
                value: wageMultiplier,
                paramName: nameof(wageMultiplier));
            RetailPriceMultiplier = ValidateMultiplier(
                value: retailPriceMultiplier,
                paramName: nameof(retailPriceMultiplier));
            HousingCostMultiplier = ValidateMultiplier(
                value: housingCostMultiplier,
                paramName: nameof(housingCostMultiplier));
            UtilityCostMultiplier = ValidateMultiplier(
                value: utilityCostMultiplier,
                paramName: nameof(utilityCostMultiplier));
            CostOfLivingIndex = ValidateIndex(
                value: costOfLivingIndex,
                paramName: nameof(costOfLivingIndex));
            AffordabilityIndex = ValidateIndex(
                value: affordabilityIndex,
                paramName: nameof(affordabilityIndex));
            LastEvaluatedAtUtc = EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
            UpdatedAtUtc = EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));
        }

        private static decimal ValidateMultiplier(
            decimal value,
            string paramName)
        {
            if (value is < 0.40m or > 2.50m)
                throw new ArgumentOutOfRangeException(paramName);

            return decimal.Round(
                d: value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal ValidateIndex(
            decimal value,
            string paramName)
        {
            if (value is < 0.20m or > 3m)
                throw new ArgumentOutOfRangeException(paramName);

            return decimal.Round(
                d: value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
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
