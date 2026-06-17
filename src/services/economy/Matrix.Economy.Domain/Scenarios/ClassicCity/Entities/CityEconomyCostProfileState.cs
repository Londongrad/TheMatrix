using Matrix.BuildingBlocks.Domain;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityEconomyCostProfileState
    {
        private CityEconomyCostProfileState() { }

        private CityEconomyCostProfileState(
            Guid cityId,
            decimal baseWageMultiplier,
            decimal baseRetailPriceMultiplier,
            decimal baseHousingCostMultiplier,
            decimal baseUtilityCostMultiplier,
            decimal wageMultiplier,
            decimal retailPriceMultiplier,
            decimal housingCostMultiplier,
            decimal utilityCostMultiplier,
            decimal costOfLivingIndex,
            decimal affordabilityIndex,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            CityId = GuardHelper.AgainstEmptyGuid(
                id: cityId,
                propertyName: nameof(cityId));
            BaseWageMultiplier = ValidateMultiplier(
                value: baseWageMultiplier,
                paramName: nameof(baseWageMultiplier));
            BaseRetailPriceMultiplier = ValidateMultiplier(
                value: baseRetailPriceMultiplier,
                paramName: nameof(baseRetailPriceMultiplier));
            BaseHousingCostMultiplier = ValidateMultiplier(
                value: baseHousingCostMultiplier,
                paramName: nameof(baseHousingCostMultiplier));
            BaseUtilityCostMultiplier = ValidateMultiplier(
                value: baseUtilityCostMultiplier,
                paramName: nameof(baseUtilityCostMultiplier));
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

        public Guid CityId { get; private set; }
        public decimal BaseWageMultiplier { get; private set; }
        public decimal BaseRetailPriceMultiplier { get; private set; }
        public decimal BaseHousingCostMultiplier { get; private set; }
        public decimal BaseUtilityCostMultiplier { get; private set; }
        public decimal WageMultiplier { get; private set; }
        public decimal RetailPriceMultiplier { get; private set; }
        public decimal HousingCostMultiplier { get; private set; }
        public decimal UtilityCostMultiplier { get; private set; }
        public decimal CostOfLivingIndex { get; private set; }
        public decimal AffordabilityIndex { get; private set; }
        public DateTimeOffset LastEvaluatedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityEconomyCostProfileState Create(
            Guid cityId,
            CityEconomyCostProfileSnapshot seed,
            DateTimeOffset updatedAtUtc)
        {
            return new CityEconomyCostProfileState(
                cityId: cityId,
                baseWageMultiplier: seed.WageMultiplier,
                baseRetailPriceMultiplier: seed.RetailPriceMultiplier,
                baseHousingCostMultiplier: seed.HousingCostMultiplier,
                baseUtilityCostMultiplier: seed.UtilityCostMultiplier,
                wageMultiplier: seed.WageMultiplier,
                retailPriceMultiplier: seed.RetailPriceMultiplier,
                housingCostMultiplier: seed.HousingCostMultiplier,
                utilityCostMultiplier: seed.UtilityCostMultiplier,
                costOfLivingIndex: seed.CostOfLivingIndex,
                affordabilityIndex: seed.AffordabilityIndex,
                lastEvaluatedAtUtc: seed.EvaluatedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void ApplySnapshot(
            CityEconomyCostProfileSnapshot snapshot,
            DateTimeOffset updatedAtUtc)
        {
            WageMultiplier = ValidateMultiplier(
                value: snapshot.WageMultiplier,
                paramName: nameof(snapshot.WageMultiplier));
            RetailPriceMultiplier = ValidateMultiplier(
                value: snapshot.RetailPriceMultiplier,
                paramName: nameof(snapshot.RetailPriceMultiplier));
            HousingCostMultiplier = ValidateMultiplier(
                value: snapshot.HousingCostMultiplier,
                paramName: nameof(snapshot.HousingCostMultiplier));
            UtilityCostMultiplier = ValidateMultiplier(
                value: snapshot.UtilityCostMultiplier,
                paramName: nameof(snapshot.UtilityCostMultiplier));
            CostOfLivingIndex = ValidateIndex(
                value: snapshot.CostOfLivingIndex,
                paramName: nameof(snapshot.CostOfLivingIndex));
            AffordabilityIndex = ValidateIndex(
                value: snapshot.AffordabilityIndex,
                paramName: nameof(snapshot.AffordabilityIndex));
            LastEvaluatedAtUtc = EnsureUtc(
                value: snapshot.EvaluatedAtUtc,
                paramName: nameof(snapshot.EvaluatedAtUtc));
            UpdatedAtUtc = EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));
        }

        public CityEconomyCostProfileSnapshot ToSnapshot()
        {
            return new CityEconomyCostProfileSnapshot(
                WageMultiplier: WageMultiplier,
                RetailPriceMultiplier: RetailPriceMultiplier,
                HousingCostMultiplier: HousingCostMultiplier,
                UtilityCostMultiplier: UtilityCostMultiplier,
                CostOfLivingIndex: CostOfLivingIndex,
                AffordabilityIndex: AffordabilityIndex,
                EvaluatedAtUtc: LastEvaluatedAtUtc);
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
            return value.Offset == TimeSpan.Zero
                ? value
                : throw new ArgumentException(
                    message: "Timestamp must be UTC.",
                    paramName: paramName);
        }
    }
}
