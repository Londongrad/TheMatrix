using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityResidentEconomicContext
    {
        private CityResidentEconomicContext(
            Money dailyTransferIncome,
            Money employmentIncomeBonus,
            double employmentOpportunityBonus,
            double employmentAvailabilityFactor,
            decimal retailStoreSpendShareAdjustment,
            decimal serviceSpendShareAdjustment,
            decimal municipalSpendShareAdjustment)
        {
            DailyTransferIncome = dailyTransferIncome;
            EmploymentIncomeBonus = employmentIncomeBonus;
            EmploymentOpportunityBonus = employmentOpportunityBonus;
            EmploymentAvailabilityFactor = employmentAvailabilityFactor;
            RetailStoreSpendShareAdjustment = retailStoreSpendShareAdjustment;
            ServiceSpendShareAdjustment = serviceSpendShareAdjustment;
            MunicipalSpendShareAdjustment = municipalSpendShareAdjustment;
        }

        public static CityResidentEconomicContext Neutral { get; } = new(
            dailyTransferIncome: Money.Zero,
            employmentIncomeBonus: Money.Zero,
            employmentOpportunityBonus: 0d,
            employmentAvailabilityFactor: 1d,
            retailStoreSpendShareAdjustment: 0m,
            serviceSpendShareAdjustment: 0m,
            municipalSpendShareAdjustment: 0m);

        public Money DailyTransferIncome { get; }
        public Money EmploymentIncomeBonus { get; }
        public double EmploymentOpportunityBonus { get; }
        public double EmploymentAvailabilityFactor { get; }
        public decimal RetailStoreSpendShareAdjustment { get; }
        public decimal ServiceSpendShareAdjustment { get; }
        public decimal MunicipalSpendShareAdjustment { get; }

        public static CityResidentEconomicContext Create(
            Money dailyTransferIncome,
            Money employmentIncomeBonus,
            double employmentOpportunityBonus,
            double employmentAvailabilityFactor,
            decimal retailStoreSpendShareAdjustment,
            decimal serviceSpendShareAdjustment,
            decimal municipalSpendShareAdjustment)
        {
            ArgumentNullException.ThrowIfNull(dailyTransferIncome);
            ArgumentNullException.ThrowIfNull(employmentIncomeBonus);

            if (dailyTransferIncome.IsNegative)
                throw new ArgumentOutOfRangeException(nameof(dailyTransferIncome));
            if (employmentIncomeBonus.IsNegative)
                throw new ArgumentOutOfRangeException(nameof(employmentIncomeBonus));
            if (employmentOpportunityBonus is < 0d or > 1d)
                throw new ArgumentOutOfRangeException(nameof(employmentOpportunityBonus));
            if (employmentAvailabilityFactor is < 0d or > 1d)
                throw new ArgumentOutOfRangeException(nameof(employmentAvailabilityFactor));

            ValidateShareAdjustment(
                value: retailStoreSpendShareAdjustment,
                parameterName: nameof(retailStoreSpendShareAdjustment));
            ValidateShareAdjustment(
                value: serviceSpendShareAdjustment,
                parameterName: nameof(serviceSpendShareAdjustment));
            ValidateShareAdjustment(
                value: municipalSpendShareAdjustment,
                parameterName: nameof(municipalSpendShareAdjustment));

            decimal adjustmentTotal = retailStoreSpendShareAdjustment +
                                      serviceSpendShareAdjustment +
                                      municipalSpendShareAdjustment;
            if (adjustmentTotal != 0m)
                throw new ArgumentException(
                    "Resident spend-share adjustments must preserve the total allocation.");

            return new CityResidentEconomicContext(
                dailyTransferIncome: dailyTransferIncome,
                employmentIncomeBonus: employmentIncomeBonus,
                employmentOpportunityBonus: employmentOpportunityBonus,
                employmentAvailabilityFactor: employmentAvailabilityFactor,
                retailStoreSpendShareAdjustment: retailStoreSpendShareAdjustment,
                serviceSpendShareAdjustment: serviceSpendShareAdjustment,
                municipalSpendShareAdjustment: municipalSpendShareAdjustment);
        }

        private static void ValidateShareAdjustment(
            decimal value,
            string parameterName)
        {
            if (value is < -1m or > 1m)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
