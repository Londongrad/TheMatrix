using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityBirthAutonomyPolicy(IPopulationGenerationContentCatalog contentCatalog)
    {
        private readonly IReadOnlyList<string> _maleFirstNames =
            contentCatalog.MaleFirstNames.Count == 0
                ? throw new InvalidOperationException("Population male first-name catalog must not be empty.")
                : contentCatalog.MaleFirstNames;

        private readonly IReadOnlyList<string> _femaleFirstNames =
            contentCatalog.FemaleFirstNames.Count == 0
                ? throw new InvalidOperationException("Population female first-name catalog must not be empty.")
                : contentCatalog.FemaleFirstNames;

        public IReadOnlyList<CityBirthAutonomyDecision> Plan(
            IReadOnlyCollection<Person> residents,
            DateOnly previousDate,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(residents);

            if (currentDate <= previousDate)
                return [];

            int reviewWindows = ResolveMonthlyReviewWindows(previousDate, currentDate);
            if (reviewWindows <= 0)
                return [];

            var residentsById = residents.ToDictionary(x => x.Id);
            var householdResidentCounts = residents
               .Where(x => x.IsAlive)
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(x => x.Key, x => x.Count());
            var householdChildCounts = residents
               .Where(x => x.IsAlive && x.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Youth)
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(x => x.Key, x => x.Count());

            var decisions = new List<CityBirthAutonomyDecision>();
            var blockedMothers = new HashSet<PersonId>();

            foreach ((Person mother, Person father) in GetBirthCandidates(residents, residentsById, currentDate))
            {
                if (blockedMothers.Contains(mother.Id))
                    continue;

                if (!ShouldGiveBirth(mother, father, householdResidentCounts, householdChildCounts, currentDate, reviewWindows))
                    continue;

                CityBirthAutonomyDecision decision = new(
                    MotherId: mother.Id,
                    FatherId: father.Id,
                    Newborn: CreateNewbornProfile(mother, father, currentDate));

                decisions.Add(decision);
                blockedMothers.Add(mother.Id);

                householdResidentCounts[mother.HouseholdId] =
                    householdResidentCounts.TryGetValue(mother.HouseholdId, out int currentResidentCount)
                        ? currentResidentCount + 1
                        : 1;
                householdChildCounts[mother.HouseholdId] =
                    householdChildCounts.TryGetValue(mother.HouseholdId, out int currentChildCount)
                        ? currentChildCount + 1
                        : 1;
            }

            return decisions;
        }

        private static IEnumerable<(Person Mother, Person Father)> GetBirthCandidates(
            IReadOnlyCollection<Person> residents,
            IReadOnlyDictionary<PersonId, Person> residentsById,
            DateOnly currentDate)
        {
            foreach (Person resident in residents)
            {
                if (!resident.IsAlive ||
                    resident.Sex != Sex.Female ||
                    resident.MaritalStatus != MaritalStatus.Married ||
                    resident.SpouseId is not { } spouseId)
                    continue;

                if (!residentsById.TryGetValue(spouseId, out Person? spouse) ||
                    !spouse.IsAlive ||
                    spouse.Sex != Sex.Male ||
                    spouse.MaritalStatus != MaritalStatus.Married ||
                    spouse.SpouseId != resident.Id ||
                    spouse.HouseholdId != resident.HouseholdId)
                    continue;

                int motherAge = resident.GetAge(currentDate).Years;
                int fatherAge = spouse.GetAge(currentDate).Years;
                if (motherAge is < 22 or > 42 || fatherAge is < 22 or > 60)
                    continue;

                yield return (resident, spouse);
            }
        }

        private static bool ShouldGiveBirth(
            Person mother,
            Person father,
            IReadOnlyDictionary<HouseholdId, int> householdResidentCounts,
            IReadOnlyDictionary<HouseholdId, int> householdChildCounts,
            DateOnly currentDate,
            int reviewWindows)
        {
            if (mother.LastChildbirthDate.HasValue &&
                currentDate.DayNumber - mother.LastChildbirthDate.Value.DayNumber < 330)
                return false;

            int childCount = householdChildCounts.TryGetValue(mother.HouseholdId, out int currentChildren)
                ? currentChildren
                : 0;

            if (childCount >= 4)
                return false;

            if (householdResidentCounts.TryGetValue(mother.HouseholdId, out int residentCount) &&
                residentCount >= HouseholdSize.Max)
                return false;

            double chancePerReview = ResolveBirthChancePerReview(mother, father, childCount, currentDate);
            return RollOccurs(mother.Id, father.Id, currentDate, 503, chancePerReview, reviewWindows);
        }

        private static double ResolveBirthChancePerReview(
            Person mother,
            Person father,
            int householdChildCount,
            DateOnly currentDate)
        {
            double motherHealth = Normalize(mother.Health.Value);
            double fatherHealth = Normalize(father.Health.Value);
            double motherHappiness = Normalize(mother.Happiness.Value);
            double fatherHappiness = Normalize(father.Happiness.Value);
            double motherStress = Normalize(mother.Stress.Value);
            double fatherStress = Normalize(father.Stress.Value);
            double averageSociability = Normalize(mother.Personality.Sociability + father.Personality.Sociability, 200d);
            double socialNeed = Normalize(mother.SocialNeed.Value + father.SocialNeed.Value, 200d);

            int motherAge = mother.GetAge(currentDate).Years;
            double ageFactor = motherAge switch
            {
                <= 25 => 0.014d,
                <= 32 => 0.020d,
                <= 37 => 0.014d,
                <= 42 => 0.008d,
                _ => 0.002d
            };

            double employmentFactor = father.Employment.Status == EmploymentStatus.Employed
                ? 0.004d
                : mother.Employment.Status == EmploymentStatus.Employed
                    ? 0.002d
                    : -0.002d;

            double childPenalty = householdChildCount * 0.004d;

            double chance = 0.002d
                            + ageFactor
                            + (motherHealth * 0.010d)
                            + (fatherHealth * 0.006d)
                            + (motherHappiness * 0.012d)
                            + (fatherHappiness * 0.006d)
                            + (averageSociability * 0.010d)
                            + (socialNeed * 0.006d)
                            - (motherStress * 0.016d)
                            - (fatherStress * 0.008d)
                            + employmentFactor
                            - childPenalty;

            if (mother.Health.Value < 45 || mother.Happiness.Value < 30)
                chance *= 0.40d;

            return Math.Clamp(chance, 0.0005d, 0.050d);
        }

        private NewbornProfile CreateNewbornProfile(
            Person mother,
            Person father,
            DateOnly currentDate)
        {
            Sex sex = GetStableFraction(mother.Id, father.Id, currentDate, 557) < 0.5d
                ? Sex.Male
                : Sex.Female;

            string firstName = ResolveFirstName(mother, father, sex, currentDate);
            string lastName = ResolveLastName(mother, father);
            Personality personality = ResolvePersonality(mother, father, currentDate);
            HealthLevel health = ResolveHealth(mother, father, currentDate);
            BodyWeight weight = ResolveWeight(sex, currentDate, mother, father);

            return new NewbornProfile(
                PersonId: PersonId.New(),
                Name: new PersonName(firstName: firstName, lastName: lastName),
                Sex: sex,
                Personality: personality,
                Health: health,
                Weight: weight);
        }

        private string ResolveFirstName(
            Person mother,
            Person father,
            Sex sex,
            DateOnly currentDate)
        {
            IReadOnlyList<string> pool = sex == Sex.Male
                ? _maleFirstNames
                : _femaleFirstNames;

            int index = GetStableInt(mother.Id, father.Id, currentDate, 571, pool.Count);
            return pool[index];
        }

        private static string ResolveLastName(Person mother, Person father)
        {
            if (string.Equals(mother.Name.LastName, father.Name.LastName, StringComparison.OrdinalIgnoreCase))
                return father.Name.LastName;

            return father.Name.LastName;
        }

        private static Personality ResolvePersonality(
            Person mother,
            Person father,
            DateOnly currentDate)
        {
            return Personality.Create(
                optimism: BlendTrait(mother.Personality.Optimism, father.Personality.Optimism, mother.Id, father.Id, currentDate, 601),
                discipline: BlendTrait(mother.Personality.Discipline, father.Personality.Discipline, mother.Id, father.Id, currentDate, 613),
                riskTolerance: BlendTrait(mother.Personality.RiskTolerance, father.Personality.RiskTolerance, mother.Id, father.Id, currentDate, 617),
                sociability: BlendTrait(mother.Personality.Sociability, father.Personality.Sociability, mother.Id, father.Id, currentDate, 631));
        }

        private static int BlendTrait(
            int firstTrait,
            int secondTrait,
            PersonId firstResidentId,
            PersonId secondResidentId,
            DateOnly currentDate,
            int salt)
        {
            int average = (firstTrait + secondTrait) / 2;
            int jitter = GetStableInt(firstResidentId, secondResidentId, currentDate, salt, 13) - 6;
            return Math.Clamp(average + jitter, 0, 100);
        }

        private static HealthLevel ResolveHealth(
            Person mother,
            Person father,
            DateOnly currentDate)
        {
            double parentHealth = (mother.Health.Value + father.Health.Value) / 2d;
            int jitter = GetStableInt(mother.Id, father.Id, currentDate, 659, 12);
            int value = (int)Math.Round(Math.Clamp(72d + (parentHealth * 0.22d) + jitter, 75d, 100d), MidpointRounding.AwayFromZero);
            return HealthLevel.From(value);
        }

        private static BodyWeight ResolveWeight(
            Sex sex,
            DateOnly currentDate,
            Person mother,
            Person father)
        {
            int grams = sex == Sex.Male
                ? 3200 + GetStableInt(mother.Id, father.Id, currentDate, 683, 1301)
                : 3000 + GetStableInt(mother.Id, father.Id, currentDate, 691, 1201);

            return BodyWeight.FromKilograms(grams / 1000m);
        }

        private static int ResolveMonthlyReviewWindows(DateOnly previousDate, DateOnly currentDate)
        {
            int previousWindow = previousDate.DayNumber / 30;
            int currentWindow = currentDate.DayNumber / 30;
            return Math.Clamp(currentWindow - previousWindow, 0, 6);
        }

        private static bool RollOccurs(
            PersonId firstResidentId,
            PersonId secondResidentId,
            DateOnly currentDate,
            int salt,
            double chancePerReview,
            int reviewWindows)
        {
            if (reviewWindows <= 0 || chancePerReview <= 0d)
                return false;

            double combinedChance = 1d - Math.Pow(1d - chancePerReview, reviewWindows);
            return GetStableFraction(firstResidentId, secondResidentId, currentDate, salt) < combinedChance;
        }

        private static double Normalize(int value)
        {
            return Math.Clamp(value / 100d, 0d, 1d);
        }

        private static double Normalize(int value, double divisor)
        {
            return Math.Clamp(value / divisor, 0d, 1d);
        }

        private static int GetStableInt(
            PersonId firstResidentId,
            PersonId secondResidentId,
            DateOnly currentDate,
            int salt,
            int modulus)
        {
            if (modulus <= 0)
                return 0;

            Guid first = firstResidentId.Value.CompareTo(secondResidentId.Value) <= 0
                ? firstResidentId.Value
                : secondResidentId.Value;
            Guid second = firstResidentId.Value.CompareTo(secondResidentId.Value) <= 0
                ? secondResidentId.Value
                : firstResidentId.Value;

            unchecked
            {
                int hash = 29;

                byte[] firstBytes = first.ToByteArray();
                for (int i = 0; i < firstBytes.Length; i++)
                    hash = (hash * 31) + firstBytes[i];

                byte[] secondBytes = second.ToByteArray();
                for (int i = 0; i < secondBytes.Length; i++)
                    hash = (hash * 31) + secondBytes[i];

                hash = (hash * 31) + currentDate.DayNumber;
                hash = (hash * 31) + salt;

                return (int)(Math.Abs((long)hash) % modulus);
            }
        }

        private static double GetStableFraction(
            PersonId firstResidentId,
            PersonId secondResidentId,
            DateOnly currentDate,
            int salt)
        {
            return GetStableInt(firstResidentId, secondResidentId, currentDate, salt, 10_000) / 10_000d;
        }
    }
}
