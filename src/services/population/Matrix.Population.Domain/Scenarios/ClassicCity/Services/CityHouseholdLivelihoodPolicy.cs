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
            {
                return new CityHouseholdLivelihoodProfile(
                    HousingStatus: housingStatus,
                    ResidentCount: 0,
                    AdultProviderCount: 0,
                    AdultStudentCount: 0,
                    DependentCount: 0,
                    InfantCount: 0,
                    ActiveIllnessCount: 0,
                    AverageHealth: 0d,
                    AverageEnergy: 0d,
                    AverageStress: 0d,
                    StabilityScore: 0d);
            }

            int adultProviderCount = 0;
            int adultStudentCount = 0;
            int dependentCount = 0;
            int infantCount = 0;
            int activeIllnessCount = 0;
            double healthTotal = 0d;
            double energyTotal = 0d;
            double stressTotal = 0d;

            foreach (Person resident in activeResidents)
            {
                AgeGroup ageGroup = resident.GetAgeGroup(currentDate);
                if (ageGroup is AgeGroup.Child or AgeGroup.Youth)
                    dependentCount++;

                if (resident.GetAge(currentDate).Years == 0)
                    infantCount++;

                if (ageGroup is AgeGroup.Adult or AgeGroup.Senior)
                {
                    switch (resident.Employment.Status)
                    {
                        case EmploymentStatus.Employed:
                            adultProviderCount++;
                            break;
                        case EmploymentStatus.Student:
                            adultStudentCount++;
                            break;
                    }
                }

                if (resident.HasActiveIllness)
                    activeIllnessCount++;

                healthTotal += resident.Health.Value;
                energyTotal += resident.Energy.Value;
                stressTotal += resident.Stress.Value;
            }

            double averageHealth = healthTotal / activeResidents.Length;
            double averageEnergy = energyTotal / activeResidents.Length;
            double averageStress = stressTotal / activeResidents.Length;
            double dependencyLoad = dependentCount + (infantCount * 0.8d);
            double supportStrength = Math.Clamp(
                (adultProviderCount + (adultStudentCount * 0.45d)) /
                Math.Max(1d, dependencyLoad + (activeResidents.Length * 0.5d)),
                0d,
                1d);
            double illnessBurden = activeIllnessCount / (double)activeResidents.Length;
            double crowdingBurden = Math.Clamp((activeResidents.Length - 4) * 0.08d, 0d, 0.32d);

            double stability = 0.16d
                               + (housingStatus == HousingStatus.Housed ? 0.18d : -0.10d)
                               + (supportStrength * 0.34d)
                               + (Normalize(averageHealth) * 0.12d)
                               + (Normalize(averageEnergy) * 0.10d)
                               - (Normalize(averageStress) * 0.14d)
                               - (illnessBurden * 0.12d)
                               - crowdingBurden
                               - (dependencyLoad * 0.03d);

            if (adultProviderCount == 0 && adultStudentCount == 0)
                stability -= 0.10d;

            return new CityHouseholdLivelihoodProfile(
                HousingStatus: housingStatus,
                ResidentCount: activeResidents.Length,
                AdultProviderCount: adultProviderCount,
                AdultStudentCount: adultStudentCount,
                DependentCount: dependentCount,
                InfantCount: infantCount,
                ActiveIllnessCount: activeIllnessCount,
                AverageHealth: averageHealth,
                AverageEnergy: averageEnergy,
                AverageStress: averageStress,
                StabilityScore: Math.Clamp(stability, 0d, 1d));
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

            double selfReliance = employmentStrength
                                  + (Normalize(resident.Health.Value) * 0.14d)
                                  + (Normalize(resident.Energy.Value) * 0.10d)
                                  + (Normalize(resident.Personality.Discipline) * 0.10d)
                                  + (Normalize(resident.Personality.Optimism) * 0.06d)
                                  - (Normalize(resident.Stress.Value) * 0.14d);

            return Math.Clamp(selfReliance, 0d, 1d);
        }

        private static double Normalize(double value)
        {
            return Math.Clamp(value / 100d, 0d, 1d);
        }
    }
}
