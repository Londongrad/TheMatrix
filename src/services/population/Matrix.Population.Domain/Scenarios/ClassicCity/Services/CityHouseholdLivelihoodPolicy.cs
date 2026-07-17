using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdLivelihoodPolicy
    {
        public CityHouseholdLivelihoodProfile Build(
            IReadOnlyCollection<Person> householdResidents,
            HousingStatus? housingStatus,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(householdResidents);

            Person[] activeResidents = householdResidents
               .Where(x => x.IsAlive)
               .ToArray();

            if (activeResidents.Length == 0)
                return new CityHouseholdLivelihoodProfile(
                    HousingStatus: housingStatus,
                    ResidentCount: 0,
                    AdultProviderCount: 0,
                    AdultStructuredParticipantCount: 0,
                    DependentCount: 0,
                    InfantCount: 0,
                    FunctionalLimitationCount: 0,
                    AverageHealth: 0d,
                    AverageEnergy: 0d,
                    AverageStress: 0d,
                    StabilityScore: 0d);

            int adultProviderCount = 0;
            int adultStructuredParticipantCount = 0;
            int dependentCount = 0;
            int infantCount = 0;
            int functionalLimitationCount = 0;
            double healthTotal = 0d;
            double energyTotal = 0d;
            double stressTotal = 0d;

            foreach (Person resident in activeResidents)
            {
                AgeGroup ageGroup = resident.GetAgeGroup(currentDate);
                if (ageGroup is AgeGroup.Child or AgeGroup.Youth)
                    dependentCount++;

                if (resident.GetAge(currentDate)
                       .Years ==
                    0)
                    infantCount++;

                if (ageGroup is AgeGroup.Adult or AgeGroup.Senior)
                    switch (resident.Employment.Status)
                    {
                        case EmploymentStatus.Employed:
                            adultProviderCount++;
                            break;
                        case EmploymentStatus.Student:
                            adultStructuredParticipantCount++;
                            break;
                    }

                if (resident.FunctionalCapacity.Value < 100)
                    functionalLimitationCount++;

                healthTotal += resident.Health.Value;
                energyTotal += resident.Energy.Value;
                stressTotal += resident.Stress.Value;
            }

            double averageHealth = healthTotal / activeResidents.Length;
            double averageEnergy = energyTotal / activeResidents.Length;
            double averageStress = stressTotal / activeResidents.Length;
            double dependencyLoad = dependentCount + (infantCount * 0.8d);
            double supportStrength = Math.Clamp(
                value: (adultProviderCount + (adultStructuredParticipantCount * 0.45d)) /
                       Math.Max(
                           val1: 1d,
                           val2: dependencyLoad + (activeResidents.Length * 0.5d)),
                min: 0d,
                max: 1d);
            double functionalBurden = functionalLimitationCount / (double)activeResidents.Length;
            double crowdingBurden = Math.Clamp(
                value: (activeResidents.Length - 4) * 0.08d,
                min: 0d,
                max: 0.32d);

            double stability = 0.16d +
                               (housingStatus == HousingStatus.Housed
                                   ? 0.18d
                                   : -0.10d) +
                               (supportStrength * 0.34d) +
                               (Normalize(averageHealth) * 0.12d) +
                               (Normalize(averageEnergy) * 0.10d) -
                               (Normalize(averageStress) * 0.14d) -
                               (functionalBurden * 0.12d) -
                               crowdingBurden -
                               (dependencyLoad * 0.03d);

            if (adultProviderCount == 0 && adultStructuredParticipantCount == 0)
                stability -= 0.10d;

            return new CityHouseholdLivelihoodProfile(
                HousingStatus: housingStatus,
                ResidentCount: activeResidents.Length,
                AdultProviderCount: adultProviderCount,
                AdultStructuredParticipantCount: adultStructuredParticipantCount,
                DependentCount: dependentCount,
                InfantCount: infantCount,
                FunctionalLimitationCount: functionalLimitationCount,
                AverageHealth: averageHealth,
                AverageEnergy: averageEnergy,
                AverageStress: averageStress,
                StabilityScore: Math.Clamp(
                    value: stability,
                    min: 0d,
                    max: 1d));
        }

        public double ResolveResidentSelfReliance(Person resident)
        {
            ArgumentNullException.ThrowIfNull(resident);

            double employmentStrength = resident.Employment.Status switch
            {
                EmploymentStatus.Employed => 0.70d,
                EmploymentStatus.Student => 0.46d,
                EmploymentStatus.Retired => 0.24d,
                _ => 0.16d
            };

            double selfReliance = employmentStrength +
                                  (Normalize(resident.Health.Value) * 0.14d) +
                                  (Normalize(resident.Energy.Value) * 0.10d) +
                                  (Normalize(resident.Personality.Discipline) * 0.10d) +
                                  (Normalize(resident.Personality.Optimism) * 0.06d) -
                                  (Normalize(resident.Stress.Value) * 0.14d);

            return Math.Clamp(
                value: selfReliance,
                min: 0d,
                max: 1d);
        }

        private static double Normalize(double value)
        {
            return Math.Clamp(
                value: value / 100d,
                min: 0d,
                max: 1d);
        }
    }
}
