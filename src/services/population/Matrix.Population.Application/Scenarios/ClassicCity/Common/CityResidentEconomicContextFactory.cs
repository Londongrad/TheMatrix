using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Common
{
    public static class CityResidentEconomicContextFactory
    {
        public static CityResidentEconomicContext Create(
            Person resident,
            EducationParticipationProjection? educationParticipation,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(resident);

            bool isEnrolled = educationParticipation?.IsEnrolled == true;
            (decimal employmentIncomeBonus, double employmentOpportunityBonus) =
                ResolveEmploymentModifiers(educationParticipation?.CompletedStage);

            if (!isEnrolled && employmentIncomeBonus == 0m && employmentOpportunityBonus == 0d)
                return CityResidentEconomicContext.Neutral;

            decimal dailyTransferIncome = isEnrolled
                ? resident.GetAgeGroup(currentDate) is AgeGroup.Adult or AgeGroup.Senior
                    ? 10m
                    : 4m
                : 0m;

            return CityResidentEconomicContext.Create(
                dailyTransferIncome: Money.FromDecimal(dailyTransferIncome),
                employmentIncomeBonus: Money.FromDecimal(employmentIncomeBonus),
                employmentOpportunityBonus: employmentOpportunityBonus,
                employmentAvailabilityFactor: isEnrolled ? 0d : 1d,
                retailStoreSpendShareAdjustment: isEnrolled ? -0.03m : 0m,
                serviceSpendShareAdjustment: isEnrolled ? -0.01m : 0m,
                municipalSpendShareAdjustment: isEnrolled ? 0.04m : 0m);
        }

        private static (decimal incomeBonus, double opportunityBonus) ResolveEmploymentModifiers(
            string? completedStage)
        {
            return completedStage switch
            {
                "primary" => (1m, 0.003d),
                "lower-secondary" => (3m, 0.006d),
                "upper-secondary" => (6m, 0.010d),
                "vocational" => (10m, 0.018d),
                "higher" or "higher-education" => (14m, 0.024d),
                "postgraduate" => (18m, 0.028d),
                _ => (0m, 0d)
            };
        }
    }
}
