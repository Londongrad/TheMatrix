using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdEconomyPolicy(
        CityHouseholdLivelihoodPolicy householdLivelihoodPolicy)
    {
        public CityHouseholdEconomyProfile Build(
            IReadOnlyCollection<Person> householdResidents,
            HousingStatus? housingStatus,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(householdResidents);

            Person[] activeResidents = householdResidents
               .Where(x => x.IsAlive)
               .ToArray();

            if (activeResidents.Length == 0)
            {
                return new CityHouseholdEconomyProfile(
                    HousingStatus: housingStatus,
                    SupportUnits: 0d,
                    LivingCostUnits: 0d,
                    EconomicBalance: 0d,
                    StrainScore: 1d,
                    GrowthReadinessScore: 0d);
            }

            CityHouseholdLivelihoodProfile livelihood = householdLivelihoodPolicy.Build(
                householdResidents: activeResidents,
                housingStatus: housingStatus,
                currentDate: currentDate);

            int retiredAdults = activeResidents.Count(x =>
                x.GetAgeGroup(currentDate) == AgeGroup.Senior &&
                x.Employment.Status == EmploymentStatus.Retired);

            double supportUnits = (livelihood.AdultProviderCount * 1.10d)
                                  + (livelihood.AdultStudentCount * 0.42d)
                                  + (retiredAdults * 0.34d)
                                  + (livelihood.StabilityScore * 0.75d);

            double livingCostUnits = (livelihood.ResidentCount * 0.44d)
                                     + (livelihood.DependentCount * 0.30d)
                                     + (livelihood.InfantCount * 0.38d)
                                     + (livelihood.ActiveIllnessCount * 0.22d)
                                     + (housingStatus == HousingStatus.Housed ? 0.56d : 0.18d);

            double balance = supportUnits - livingCostUnits;
            double strain = Math.Clamp(
                0.50d - (balance * 0.40d),
                0d,
                1d);

            if (livelihood.AdultProviderCount == 0 && livelihood.AdultStudentCount == 0)
                strain = Math.Clamp(strain + 0.12d, 0d, 1d);

            double growthReadiness = Math.Clamp(
                (0.55d - strain) + (livelihood.StabilityScore * 0.45d),
                0d,
                1d);

            return new CityHouseholdEconomyProfile(
                HousingStatus: housingStatus,
                SupportUnits: supportUnits,
                LivingCostUnits: livingCostUnits,
                EconomicBalance: balance,
                StrainScore: strain,
                GrowthReadinessScore: growthReadiness);
        }
    }
}
