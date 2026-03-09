using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityEmploymentAutonomyPolicy(
        IPopulationGenerationContentCatalog contentCatalog,
        CityHouseholdEconomyPolicy householdEconomyPolicy)
    {
        private readonly IReadOnlyList<PopulationProfessionCatalogItem> _professions =
            contentCatalog.Professions.Count == 0
                ? throw new InvalidOperationException("Population profession catalog must not be empty.")
                : contentCatalog.Professions;

        public bool Apply(
            Person person,
            IReadOnlyCollection<Person> householdResidents,
            DateOnly previousDate,
            DateOnly currentDate,
            HousingStatus? housingStatus,
            IDictionary<string, List<WorkplaceId>> workplacePools)
        {
            ArgumentNullException.ThrowIfNull(person);
            ArgumentNullException.ThrowIfNull(householdResidents);
            ArgumentNullException.ThrowIfNull(workplacePools);

            if (!person.IsAlive)
                return false;

            if (currentDate <= previousDate)
                return false;

            AgeGroup currentAgeGroup = person.GetAgeGroup(currentDate);
            if (currentAgeGroup != AgeGroup.Adult)
                return false;

            int reviewWindows = ResolveReviewWindows(
                previousDate: previousDate,
                currentDate: currentDate);
            CityHouseholdEconomyProfile householdEconomy = householdEconomyPolicy.Build(
                householdResidents: householdResidents,
                housingStatus: housingStatus,
                currentDate: currentDate);

            return person.Employment.Status switch
            {
                EmploymentStatus.Unemployed or EmploymentStatus.None => TryAssignAutonomousJob(
                    person: person,
                    currentDate: currentDate,
                    reviewWindows: reviewWindows,
                    householdEconomy: householdEconomy,
                    workplacePools: workplacePools),
                EmploymentStatus.Employed => TryTriggerJobLoss(
                    person: person,
                    currentDate: currentDate,
                    reviewWindows: reviewWindows,
                    householdEconomy: householdEconomy),
                _ => false
            };
        }

        private bool TryAssignAutonomousJob(
            Person person,
            DateOnly currentDate,
            int reviewWindows,
            CityHouseholdEconomyProfile householdEconomy,
            IDictionary<string, List<WorkplaceId>> workplacePools)
        {
            double chancePerReview = ResolveHireChancePerReview(person, householdEconomy);
            if (!RollOccurs(
                    personId: person.Id,
                    currentDate: currentDate,
                    salt: 17,
                    chancePerReview: chancePerReview,
                    reviewWindows: reviewWindows))
                return false;

            PopulationProfessionCatalogItem profession = PickProfession(
                personId: person.Id,
                currentDate: currentDate);
            WorkplaceId workplaceId = ResolveWorkplaceId(
                person: person,
                currentDate: currentDate,
                jobTitle: profession.Title,
                workplacePools: workplacePools);

            person.AssignJob(
                currentDate: currentDate,
                job: new Job(
                    workplaceId: workplaceId,
                    title: profession.Title));

            return true;
        }

        private static bool TryTriggerJobLoss(
            Person person,
            DateOnly currentDate,
            int reviewWindows,
            CityHouseholdEconomyProfile householdEconomy)
        {
            double chancePerReview = ResolveJobLossChancePerReview(person, householdEconomy);
            if (!RollOccurs(
                    personId: person.Id,
                    currentDate: currentDate,
                    salt: 41,
                    chancePerReview: chancePerReview,
                    reviewWindows: reviewWindows))
                return false;

            person.Fire(currentDate);
            return true;
        }

        private WorkplaceId ResolveWorkplaceId(
            Person person,
            DateOnly currentDate,
            string jobTitle,
            IDictionary<string, List<WorkplaceId>> workplacePools)
        {
            if (!workplacePools.TryGetValue(jobTitle, out List<WorkplaceId>? titlePool))
            {
                titlePool = [];
                workplacePools[jobTitle] = titlePool;
            }

            bool shouldCreateNew = titlePool.Count == 0 ||
                                   (titlePool.Count < 12 &&
                                    GetStableFraction(
                                        personId: person.Id,
                                        currentDate: currentDate,
                                        salt: 73) < 0.18d);

            if (shouldCreateNew)
            {
                WorkplaceId created = WorkplaceId.New();
                titlePool.Add(created);
                return created;
            }

            int stableIndex = GetStableInt(
                personId: person.Id,
                currentDate: currentDate,
                salt: 97,
                modulus: titlePool.Count);

            return titlePool[stableIndex];
        }

        private PopulationProfessionCatalogItem PickProfession(
            PersonId personId,
            DateOnly currentDate)
        {
            int totalWeight = 0;
            for (int i = 0; i < _professions.Count; i++)
                totalWeight += _professions[i].Weight;

            int roll = GetStableInt(
                personId: personId,
                currentDate: currentDate,
                salt: 131,
                modulus: totalWeight);
            int accumulated = 0;

            for (int i = 0; i < _professions.Count; i++)
            {
                PopulationProfessionCatalogItem profession = _professions[i];
                accumulated += profession.Weight;
                if (roll < accumulated)
                    return profession;
            }

            return _professions[^1];
        }

        private static int ResolveReviewWindows(
            DateOnly previousDate,
            DateOnly currentDate)
        {
            int previousWindow = previousDate.DayNumber / 7;
            int currentWindow = currentDate.DayNumber / 7;
            return Math.Clamp(currentWindow - previousWindow, 0, 8);
        }

        private static double ResolveHireChancePerReview(
            Person person,
            CityHouseholdEconomyProfile householdEconomy)
        {
            double discipline = Normalize(person.Personality.Discipline);
            double optimism = Normalize(person.Personality.Optimism);
            double health = Normalize(person.Health.Value);
            double energy = Normalize(person.Energy.Value);
            double stress = Normalize(person.Stress.Value);

            double educationBonus = person.EducationLevel switch
            {
                EducationLevel.None => 0.000d,
                EducationLevel.Preschool => 0.000d,
                EducationLevel.Primary => 0.003d,
                EducationLevel.LowerSecondary => 0.006d,
                EducationLevel.UpperSecondary => 0.010d,
                EducationLevel.Vocational => 0.018d,
                EducationLevel.Higher => 0.024d,
                EducationLevel.Postgraduate => 0.028d,
                _ => 0.006d
            };

            double chance = 0.010d
                            + (discipline * 0.030d)
                            + (optimism * 0.015d)
                            + (health * 0.020d)
                            + (energy * 0.020d)
                            - (stress * 0.030d)
                            + educationBonus;

            chance += householdEconomy.StrainScore * 0.030d;
            chance -= Math.Max(0d, householdEconomy.EconomicBalance) * 0.006d;

            if (person.Health.Value < 25 || person.Energy.Value < 20)
                chance *= 0.40d;

            return Math.Clamp(chance, 0.003d, 0.120d);
        }

        private static double ResolveJobLossChancePerReview(
            Person person,
            CityHouseholdEconomyProfile householdEconomy)
        {
            double discipline = Normalize(person.Personality.Discipline);
            double optimism = Normalize(person.Personality.Optimism);
            double stress = Normalize(person.Stress.Value);
            double lowHealth = 1d - Normalize(person.Health.Value);
            double lowEnergy = 1d - Normalize(person.Energy.Value);

            double chance = 0.002d
                            + (stress * 0.020d)
                            + (lowHealth * 0.015d)
                            + (lowEnergy * 0.012d)
                            - (discipline * 0.010d)
                            - (optimism * 0.005d);

            chance -= householdEconomy.StrainScore * 0.010d;
            chance += Math.Max(0d, householdEconomy.EconomicBalance) * 0.004d;

            if (person.Health.Value < 20 || person.Energy.Value < 15 || person.Stress.Value > 90)
                chance += 0.020d;

            return Math.Clamp(chance, 0.001d, 0.090d);
        }

        private static bool RollOccurs(
            PersonId personId,
            DateOnly currentDate,
            int salt,
            double chancePerReview,
            int reviewWindows)
        {
            if (reviewWindows <= 0 || chancePerReview <= 0d)
                return false;

            double combinedChance = 1d - Math.Pow(1d - chancePerReview, reviewWindows);
            return GetStableFraction(
                personId: personId,
                currentDate: currentDate,
                salt: salt) < combinedChance;
        }

        private static double Normalize(int value)
        {
            return Math.Clamp(value / 100d, 0d, 1d);
        }

        private static int GetStableInt(
            PersonId personId,
            DateOnly currentDate,
            int salt,
            int modulus)
        {
            if (modulus <= 0)
                return 0;

            unchecked
            {
                byte[] bytes = personId.Value.ToByteArray();
                int hash = 17;
                for (int i = 0; i < bytes.Length; i++)
                    hash = (hash * 31) + bytes[i];

                hash = (hash * 31) + currentDate.DayNumber;
                hash = (hash * 31) + salt;

                return (int)(Math.Abs((long)hash) % modulus);
            }
        }

        private static double GetStableFraction(
            PersonId personId,
            DateOnly currentDate,
            int salt)
        {
            return GetStableInt(
                       personId: personId,
                       currentDate: currentDate,
                       salt: salt,
                       modulus: 10_000) / 10_000d;
        }
    }
}
