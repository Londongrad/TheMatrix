using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Integration;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Common
{
    public static class CityResidentEconomicContextFactory
    {
        public static CityResidentEconomicContext Create(
            Person resident,
            ResidentExternalActivityProfile? externalActivity,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(resident);

            if (externalActivity?.ResidentLifecycleRevision != resident.LifecycleRevision)
                externalActivity = null;

            bool hasExternalActivity = externalActivity?.HasStructuredActivity == true;
            (decimal employmentIncomeBonus, double employmentOpportunityBonus) =
                ResolveEmploymentModifiers(
                    externalActivity?.WorkforceQualification ?? ResidentWorkforceQualificationTier.None);

            if (!hasExternalActivity && employmentIncomeBonus == 0m && employmentOpportunityBonus == 0d)
                return CityResidentEconomicContext.Neutral;

            decimal dailyTransferIncome = hasExternalActivity
                ? resident.GetAgeGroup(currentDate) is AgeGroup.Adult or AgeGroup.Senior
                    ? 10m
                    : 4m
                : 0m;

            return CityResidentEconomicContext.Create(
                dailyTransferIncome: Money.FromDecimal(dailyTransferIncome),
                employmentIncomeBonus: Money.FromDecimal(employmentIncomeBonus),
                employmentOpportunityBonus: employmentOpportunityBonus,
                employmentAvailabilityFactor: hasExternalActivity ? 0d : 1d,
                retailStoreSpendShareAdjustment: hasExternalActivity ? -0.03m : 0m,
                serviceSpendShareAdjustment: hasExternalActivity ? -0.01m : 0m,
                municipalSpendShareAdjustment: hasExternalActivity ? 0.04m : 0m);
        }

        private static (decimal incomeBonus, double opportunityBonus) ResolveEmploymentModifiers(
            ResidentWorkforceQualificationTier qualification)
        {
            return qualification switch
            {
                ResidentWorkforceQualificationTier.None => (0m, 0d),
                ResidentWorkforceQualificationTier.Entry => (1m, 0.003d),
                ResidentWorkforceQualificationTier.Basic => (3m, 0.006d),
                ResidentWorkforceQualificationTier.General => (6m, 0.010d),
                ResidentWorkforceQualificationTier.Skilled => (10m, 0.018d),
                ResidentWorkforceQualificationTier.Professional => (14m, 0.024d),
                ResidentWorkforceQualificationTier.Specialist => (18m, 0.028d),
                _ => throw new ArgumentOutOfRangeException(nameof(qualification))
            };
        }
    }
}
