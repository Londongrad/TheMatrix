using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityBirthAutonomyPolicy(
        IPopulationGenerationContentCatalog contentCatalog,
        CityHouseholdLivelihoodPolicy householdLivelihoodPolicy)
    {
        private readonly IReadOnlyList<string> _femaleFirstNames =
            contentCatalog.FemaleFirstNames.Count == 0
                ? throw new InvalidOperationException("Population female first-name catalog must not be empty.")
                : contentCatalog.FemaleFirstNames;

        private readonly IReadOnlyList<string> _maleFirstNames =
            contentCatalog.MaleFirstNames.Count == 0
                ? throw new InvalidOperationException("Population male first-name catalog must not be empty.")
                : contentCatalog.MaleFirstNames;

        public IReadOnlyList<CityBirthAutonomyDecision> Plan(
            IReadOnlyCollection<Person> residents,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingStatuses,
            DateOnly previousDate,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(residents);
            ArgumentNullException.ThrowIfNull(housingStatuses);

            if (currentDate <= previousDate)
                return [];

            int reviewWindows = ResolveMonthlyReviewWindows(
                previousDate: previousDate,
                currentDate: currentDate);
            if (reviewWindows <= 0)
                return [];

            var residentsById = residents.ToDictionary(x => x.Id);
            var householdResidents = residents
               .Where(x => x.IsAlive)
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => (IReadOnlyCollection<Person>)x.ToList());
            var householdResidentCounts = residents
               .Where(x => x.IsAlive)
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.Count());
            var householdChildCounts = residents
               .Where(x => x.IsAlive && x.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Youth)
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.Count());

            var decisions = new List<CityBirthAutonomyDecision>();
            var blockedMothers = new HashSet<PersonId>();

            foreach ((Person mother, Person father) in GetBirthCandidates(
                         residents: residents,
                         residentsById: residentsById,
                         currentDate: currentDate))
            {
                if (blockedMothers.Contains(mother.Id))
                    continue;

                if (!householdResidents.TryGetValue(
                        key: mother.HouseholdId,
                        value: out IReadOnlyCollection<Person>? members))
                    continue;

                HousingStatus? housingStatus = housingStatuses.TryGetValue(
                    key: mother.HouseholdId,
                    value: out HousingStatus resolvedHousingStatus)
                    ? resolvedHousingStatus
                    : null;
                CityHouseholdLivelihoodProfile livelihoodProfile = householdLivelihoodPolicy.Build(
                    householdResidents: members,
                    housingStatus: housingStatus,
                    currentDate: currentDate);

                if (!ShouldGiveBirth(
                        mother: mother,
                        father: father,
                        livelihoodProfile: livelihoodProfile,
                        householdResidentCounts: householdResidentCounts,
                        householdChildCounts: householdChildCounts,
                        currentDate: currentDate,
                        reviewWindows: reviewWindows))
                    continue;

                CityBirthAutonomyDecision decision = new(
                    MotherId: mother.Id,
                    FatherId: father.Id,
                    Newborn: CreateNewbornProfile(
                        mother: mother,
                        father: father,
                        currentDate: currentDate));

                decisions.Add(decision);
                blockedMothers.Add(mother.Id);

                householdResidentCounts[mother.HouseholdId] =
                    householdResidentCounts.TryGetValue(
                        key: mother.HouseholdId,
                        value: out int currentResidentCount)
                        ? currentResidentCount + 1
                        : 1;
                householdChildCounts[mother.HouseholdId] =
                    householdChildCounts.TryGetValue(
                        key: mother.HouseholdId,
                        value: out int currentChildCount)
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
                    resident.SpouseId is not
                        { } spouseId)
                    continue;

                if (!residentsById.TryGetValue(
                        key: spouseId,
                        value: out Person? spouse) ||
                    !spouse.IsAlive ||
                    spouse.Sex != Sex.Male ||
                    spouse.MaritalStatus != MaritalStatus.Married ||
                    spouse.SpouseId != resident.Id ||
                    spouse.HouseholdId != resident.HouseholdId)
                    continue;

                int motherAge = resident.GetAge(currentDate)
                   .Years;
                int fatherAge = spouse.GetAge(currentDate)
                   .Years;
                if (motherAge is < 22 or > 42 || fatherAge is < 22 or > 60)
                    continue;

                yield return (resident, spouse);
            }
        }

        private static bool ShouldGiveBirth(
            Person mother,
            Person father,
            CityHouseholdLivelihoodProfile livelihoodProfile,
            IReadOnlyDictionary<HouseholdId, int> householdResidentCounts,
            IReadOnlyDictionary<HouseholdId, int> householdChildCounts,
            DateOnly currentDate,
            int reviewWindows)
        {
            if (mother.LastChildbirthDate.HasValue &&
                currentDate.DayNumber - mother.LastChildbirthDate.Value.DayNumber < 330)
                return false;

            int childCount = householdChildCounts.TryGetValue(
                key: mother.HouseholdId,
                value: out int currentChildren)
                ? currentChildren
                : 0;

            if (childCount >= 4)
                return false;

            if (householdResidentCounts.TryGetValue(
                    key: mother.HouseholdId,
                    value: out int residentCount) &&
                residentCount >= HouseholdSize.Max)
                return false;

            double chancePerReview = ResolveBirthChancePerReview(
                mother: mother,
                father: father,
                livelihoodProfile: livelihoodProfile,
                householdChildCount: childCount,
                currentDate: currentDate);
            return RollOccurs(
                firstResidentId: mother.Id,
                secondResidentId: father.Id,
                currentDate: currentDate,
                salt: 503,
                chancePerReview: chancePerReview,
                reviewWindows: reviewWindows);
        }

        private static double ResolveBirthChancePerReview(
            Person mother,
            Person father,
            CityHouseholdLivelihoodProfile livelihoodProfile,
            int householdChildCount,
            DateOnly currentDate)
        {
            double motherHealth = Normalize(mother.Health.Value);
            double fatherHealth = Normalize(father.Health.Value);
            double motherHappiness = Normalize(mother.Happiness.Value);
            double fatherHappiness = Normalize(father.Happiness.Value);
            double motherStress = Normalize(mother.Stress.Value);
            double fatherStress = Normalize(father.Stress.Value);
            double averageSociability = Normalize(
                value: mother.Personality.Sociability + father.Personality.Sociability,
                divisor: 200d);
            double socialNeed = Normalize(
                value: mother.SocialNeed.Value + father.SocialNeed.Value,
                divisor: 200d);

            int motherAge = mother.GetAge(currentDate)
               .Years;
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

            double livelihoodFactor = Math.Clamp(
                value: 0.35d + (livelihoodProfile.StabilityScore * 0.95d),
                min: 0.20d,
                max: 1.20d);

            double chance = 0.002d +
                            ageFactor +
                            (motherHealth * 0.010d) +
                            (fatherHealth * 0.006d) +
                            (motherHappiness * 0.012d) +
                            (fatherHappiness * 0.006d) +
                            (averageSociability * 0.010d) +
                            (socialNeed * 0.006d) -
                            (motherStress * 0.016d) -
                            (fatherStress * 0.008d) +
                            employmentFactor -
                            childPenalty;

            chance *= livelihoodFactor;

            if (!livelihoodProfile.IsHoused)
                chance *= 0.55d;

            if (!livelihoodProfile.HasStructuredSupport)
                chance *= 0.55d;

            if (mother.Health.Value < 45 || mother.Happiness.Value < 30)
                chance *= 0.40d;

            return Math.Clamp(
                value: chance,
                min: 0.0005d,
                max: 0.050d);
        }

        private NewbornProfile CreateNewbornProfile(
            Person mother,
            Person father,
            DateOnly currentDate)
        {
            Sex sex = GetStableFraction(
                          firstResidentId: mother.Id,
                          secondResidentId: father.Id,
                          currentDate: currentDate,
                          salt: 557) <
                      0.5d
                ? Sex.Male
                : Sex.Female;

            string firstName = ResolveFirstName(
                mother: mother,
                father: father,
                sex: sex,
                currentDate: currentDate);
            string lastName = ResolveLastName(
                mother: mother,
                father: father);
            Personality personality = ResolvePersonality(
                mother: mother,
                father: father,
                currentDate: currentDate);
            HealthLevel health = ResolveHealth(
                mother: mother,
                father: father,
                currentDate: currentDate);
            BodyWeight weight = ResolveWeight(
                sex: sex,
                currentDate: currentDate,
                mother: mother,
                father: father);

            return new NewbornProfile(
                PersonId: PersonId.New(),
                Name: new PersonName(
                    firstName: firstName,
                    lastName: lastName),
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

            int index = GetStableInt(
                firstResidentId: mother.Id,
                secondResidentId: father.Id,
                currentDate: currentDate,
                salt: 571,
                modulus: pool.Count);
            return pool[index];
        }

        private static string ResolveLastName(
            Person mother,
            Person father)
        {
            if (string.Equals(
                    a: mother.Name.LastName,
                    b: father.Name.LastName,
                    comparisonType: StringComparison.OrdinalIgnoreCase))
                return father.Name.LastName;

            return father.Name.LastName;
        }

        private static Personality ResolvePersonality(
            Person mother,
            Person father,
            DateOnly currentDate)
        {
            return Personality.Create(
                optimism: BlendTrait(
                    firstTrait: mother.Personality.Optimism,
                    secondTrait: father.Personality.Optimism,
                    firstResidentId: mother.Id,
                    secondResidentId: father.Id,
                    currentDate: currentDate,
                    salt: 601),
                discipline: BlendTrait(
                    firstTrait: mother.Personality.Discipline,
                    secondTrait: father.Personality.Discipline,
                    firstResidentId: mother.Id,
                    secondResidentId: father.Id,
                    currentDate: currentDate,
                    salt: 613),
                riskTolerance: BlendTrait(
                    firstTrait: mother.Personality.RiskTolerance,
                    secondTrait: father.Personality.RiskTolerance,
                    firstResidentId: mother.Id,
                    secondResidentId: father.Id,
                    currentDate: currentDate,
                    salt: 617),
                sociability: BlendTrait(
                    firstTrait: mother.Personality.Sociability,
                    secondTrait: father.Personality.Sociability,
                    firstResidentId: mother.Id,
                    secondResidentId: father.Id,
                    currentDate: currentDate,
                    salt: 631));
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
            int jitter = GetStableInt(
                             firstResidentId: firstResidentId,
                             secondResidentId: secondResidentId,
                             currentDate: currentDate,
                             salt: salt,
                             modulus: 13) -
                         6;
            return Math.Clamp(
                value: average + jitter,
                min: 0,
                max: 100);
        }

        private static HealthLevel ResolveHealth(
            Person mother,
            Person father,
            DateOnly currentDate)
        {
            double parentHealth = (mother.Health.Value + father.Health.Value) / 2d;
            int jitter = GetStableInt(
                firstResidentId: mother.Id,
                secondResidentId: father.Id,
                currentDate: currentDate,
                salt: 659,
                modulus: 12);
            int value = (int)Math.Round(
                value: Math.Clamp(
                    value: 72d + (parentHealth * 0.22d) + jitter,
                    min: 75d,
                    max: 100d),
                mode: MidpointRounding.AwayFromZero);
            return HealthLevel.From(value);
        }

        private static BodyWeight ResolveWeight(
            Sex sex,
            DateOnly currentDate,
            Person mother,
            Person father)
        {
            int grams = sex == Sex.Male
                ? 3200 +
                GetStableInt(
                    firstResidentId: mother.Id,
                    secondResidentId: father.Id,
                    currentDate: currentDate,
                    salt: 683,
                    modulus: 1301)
                : 3000 +
                GetStableInt(
                    firstResidentId: mother.Id,
                    secondResidentId: father.Id,
                    currentDate: currentDate,
                    salt: 691,
                    modulus: 1201);

            return BodyWeight.FromKilograms(grams / 1000m);
        }

        private static int ResolveMonthlyReviewWindows(
            DateOnly previousDate,
            DateOnly currentDate)
        {
            int previousWindow = previousDate.DayNumber / 30;
            int currentWindow = currentDate.DayNumber / 30;
            return Math.Clamp(
                value: currentWindow - previousWindow,
                min: 0,
                max: 6);
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

            double combinedChance = 1d -
                Math.Pow(
                    x: 1d - chancePerReview,
                    y: reviewWindows);
            return GetStableFraction(
                       firstResidentId: firstResidentId,
                       secondResidentId: secondResidentId,
                       currentDate: currentDate,
                       salt: salt) <
                   combinedChance;
        }

        private static double Normalize(int value)
        {
            return Math.Clamp(
                value: value / 100d,
                min: 0d,
                max: 1d);
        }

        private static double Normalize(
            int value,
            double divisor)
        {
            return Math.Clamp(
                value: value / divisor,
                min: 0d,
                max: 1d);
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
            return GetStableInt(
                       firstResidentId: firstResidentId,
                       secondResidentId: secondResidentId,
                       currentDate: currentDate,
                       salt: salt,
                       modulus: 10_000) /
                   10_000d;
        }
    }
}
