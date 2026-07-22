using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Integration
{
    public sealed record ResidentExternalEconomicProfile
    {
        public ResidentExternalEconomicProfile(
            ResidentAgeIncomeSchedule transferIncome,
            decimal employmentIncomeBonus = 0m,
            double employmentOpportunityBonus = 0d,
            double employmentAvailabilityFactor = 1d,
            decimal retailStoreSpendShareAdjustment = 0m,
            decimal serviceSpendShareAdjustment = 0m,
            decimal municipalSpendShareAdjustment = 0m)
        {
            ArgumentNullException.ThrowIfNull(transferIncome);
            if (employmentIncomeBonus < 0m)
                throw new ArgumentOutOfRangeException(nameof(employmentIncomeBonus));
            if (!double.IsFinite(employmentOpportunityBonus) || employmentOpportunityBonus is < 0d or > 1d)
                throw new ArgumentOutOfRangeException(nameof(employmentOpportunityBonus));
            if (!double.IsFinite(employmentAvailabilityFactor) || employmentAvailabilityFactor is < 0d or > 1d)
                throw new ArgumentOutOfRangeException(nameof(employmentAvailabilityFactor));
            if (retailStoreSpendShareAdjustment is < -1m or > 1m)
                throw new ArgumentOutOfRangeException(nameof(retailStoreSpendShareAdjustment));
            if (serviceSpendShareAdjustment is < -1m or > 1m)
                throw new ArgumentOutOfRangeException(nameof(serviceSpendShareAdjustment));
            if (municipalSpendShareAdjustment is < -1m or > 1m)
                throw new ArgumentOutOfRangeException(nameof(municipalSpendShareAdjustment));
            if (retailStoreSpendShareAdjustment + serviceSpendShareAdjustment + municipalSpendShareAdjustment != 0m)
                throw new ArgumentException("Spend-share adjustments must preserve the total allocation.");

            TransferIncome = transferIncome;
            EmploymentIncomeBonus = employmentIncomeBonus;
            EmploymentOpportunityBonus = employmentOpportunityBonus;
            EmploymentAvailabilityFactor = employmentAvailabilityFactor;
            RetailStoreSpendShareAdjustment = retailStoreSpendShareAdjustment;
            ServiceSpendShareAdjustment = serviceSpendShareAdjustment;
            MunicipalSpendShareAdjustment = municipalSpendShareAdjustment;
        }

        public static ResidentExternalEconomicProfile Neutral { get; } = new(ResidentAgeIncomeSchedule.None);

        public ResidentAgeIncomeSchedule TransferIncome { get; }
        public decimal EmploymentIncomeBonus { get; }
        public double EmploymentOpportunityBonus { get; }
        public double EmploymentAvailabilityFactor { get; }
        public decimal RetailStoreSpendShareAdjustment { get; }
        public decimal ServiceSpendShareAdjustment { get; }
        public decimal MunicipalSpendShareAdjustment { get; }
    }
}
