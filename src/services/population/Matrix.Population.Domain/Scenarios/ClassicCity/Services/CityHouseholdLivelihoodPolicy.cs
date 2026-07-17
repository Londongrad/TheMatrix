using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.ValueObjects;

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

            return Build(
                householdResidents: householdResidents,
                routineProfilesByResidentId: CreateCompatibilityRoutineProfiles(householdResidents),
                housingStatus: housingStatus,
                currentDate: currentDate);
        }

        public CityHouseholdLivelihoodProfile Build(
            IReadOnlyCollection<Person> householdResidents,
            IReadOnlyDictionary<PersonId, PersonRoutineProfile> routineProfilesByResidentId,
            HousingStatus? housingStatus,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(householdResidents);
            ArgumentNullException.ThrowIfNull(routineProfilesByResidentId);

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
                {
                    if (resident.Employment.Status == EmploymentStatus.Employed)
                        adultProviderCount++;
                    else
                        if (routineProfilesByResidentId.TryGetValue(
                                key: resident.Id,
                                value: out PersonRoutineProfile? routineProfile) &&
                            routineProfile.HasStructuredActivity)
                            adultStructuredParticipantCount++;
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

        public double ResolveResidentSelfReliance(
            Person resident,
            PersonRoutineProfile routineProfile)
        {
            ArgumentNullException.ThrowIfNull(resident);
            ArgumentNullException.ThrowIfNull(routineProfile);

            double employmentStrength = resident.Employment.Status == EmploymentStatus.Employed
                ? 0.70d
                : resident.Employment.Status == EmploymentStatus.Retired
                    ? 0.24d
                    : routineProfile.HasStructuredActivity
                        ? 0.46d
                        : 0.16d;

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

        private static IReadOnlyDictionary<PersonId, PersonRoutineProfile> CreateCompatibilityRoutineProfiles(
            IEnumerable<Person> residents)
        {
            return residents
               .Where(x => x.Employment.Status == EmploymentStatus.Student)
               .ToDictionary(
                    keySelector: x => x.Id,
                    elementSelector: _ => PersonRoutineProfile.Structured(
                        activityStart: TimeSpan.FromHours(8),
                        activityEnd: TimeSpan.FromHours(15),
                        activityLoad: PersonStructuredActivityLoad.Moderate));
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
