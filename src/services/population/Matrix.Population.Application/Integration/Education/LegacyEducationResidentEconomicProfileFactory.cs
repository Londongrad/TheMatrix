using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Integration.Education
{
    internal static class LegacyEducationResidentEconomicProfileFactory
    {
        private static readonly ResidentAgeIncomeSchedule StudentIncome =
            ResidentAgeIncomeSchedule.Create((0, 4m), (17, 10m));

        private static readonly IReadOnlyDictionary<ResidentWorkforceQualificationTier,
            (ResidentExternalEconomicProfile Enrolled, ResidentExternalEconomicProfile NotEnrolled)> Profiles =
            Enum.GetValues<ResidentWorkforceQualificationTier>().ToDictionary(
                qualification => qualification,
                qualification => (Create(qualification, true), Create(qualification, false)));

        public static ResidentExternalEconomicProfile Resolve(
            bool isEnrolled,
            ResidentWorkforceQualificationTier qualification)
        {
            if (!Profiles.TryGetValue(qualification, out var profiles))
                throw new ArgumentOutOfRangeException(nameof(qualification));

            return isEnrolled ? profiles.Enrolled : profiles.NotEnrolled;
        }

        private static ResidentExternalEconomicProfile Create(
            ResidentWorkforceQualificationTier qualification,
            bool isEnrolled)
        {
            (decimal incomeBonus, double opportunityBonus) = qualification switch
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

            if (!isEnrolled && qualification == ResidentWorkforceQualificationTier.None)
                return ResidentExternalEconomicProfile.Neutral;

            return new ResidentExternalEconomicProfile(
                transferIncome: isEnrolled ? StudentIncome : ResidentAgeIncomeSchedule.None,
                employmentIncomeBonus: incomeBonus,
                employmentOpportunityBonus: opportunityBonus,
                employmentAvailabilityFactor: isEnrolled ? 0d : 1d,
                retailStoreSpendShareAdjustment: isEnrolled ? -0.03m : 0m,
                serviceSpendShareAdjustment: isEnrolled ? -0.01m : 0m,
                municipalSpendShareAdjustment: isEnrolled ? 0.04m : 0m);
        }
    }
}
