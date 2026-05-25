using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityCivilRegistryAutonomyPolicy
    {
        public IReadOnlyList<CityCivilRegistryAutonomyDecision> Plan(
            IReadOnlyCollection<Person> residents,
            DateOnly previousDate,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(residents);

            if (currentDate <= previousDate || residents.Count < 2)
                return [];

            int reviewWindows = ResolveMonthlyReviewWindows(
                previousDate: previousDate,
                currentDate: currentDate);

            if (reviewWindows <= 0)
                return [];

            var residentsById = residents.ToDictionary(
                keySelector: x => x.Id,
                elementSelector: x => x);
            var blockedResidents = new HashSet<PersonId>();
            var decisions = new List<CityCivilRegistryAutonomyDecision>();

            foreach ((Person first, Person second) in GetDivorceCandidates(
                         residents: residents,
                         residentsById: residentsById,
                         currentDate: currentDate))
            {
                if (blockedResidents.Contains(first.Id) || blockedResidents.Contains(second.Id))
                    continue;

                if (!ShouldDivorce(
                        first: first,
                        second: second,
                        currentDate: currentDate,
                        reviewWindows: reviewWindows))
                    continue;

                decisions.Add(
                    new CityCivilRegistryAutonomyDecision(
                        Type: CityCivilRegistryAutonomyDecisionType.Divorce,
                        FirstResidentId: first.Id,
                        SecondResidentId: second.Id));

                blockedResidents.Add(first.Id);
                blockedResidents.Add(second.Id);
            }

            Dictionary<int, List<Person>> marriageLanes = BuildMarriageLanes(
                residents: residents,
                blockedResidents: blockedResidents,
                currentDate: currentDate,
                reviewWindows: reviewWindows);

            foreach (int laneKey in marriageLanes.Keys.OrderBy(x => x))
            {
                List<Person> laneResidents = marriageLanes[laneKey];
                laneResidents.Sort(
                    comparison: (
                        left,
                        right) => CompareMarriageLaneResidents(
                        left: left,
                        right: right,
                        currentDate: currentDate));

                for (int i = 0; i < laneResidents.Count; i++)
                {
                    Person first = laneResidents[i];
                    if (blockedResidents.Contains(first.Id))
                        continue;

                    Person? partner = null;
                    for (int j = i + 1; j < laneResidents.Count && j <= i + 3; j++)
                    {
                        Person candidate = laneResidents[j];
                        if (blockedResidents.Contains(candidate.Id))
                            continue;

                        if (!AreMarriageCompatible(
                                first: first,
                                second: candidate,
                                currentDate: currentDate))
                            continue;

                        partner = candidate;
                        break;
                    }

                    if (partner is null)
                        continue;

                    decisions.Add(
                        new CityCivilRegistryAutonomyDecision(
                            Type: CityCivilRegistryAutonomyDecisionType.Marriage,
                            FirstResidentId: first.Id,
                            SecondResidentId: partner.Id));

                    blockedResidents.Add(first.Id);
                    blockedResidents.Add(partner.Id);
                }
            }

            return decisions;
        }

        private static IEnumerable<(Person First, Person Second)> GetDivorceCandidates(
            IReadOnlyCollection<Person> residents,
            IReadOnlyDictionary<PersonId, Person> residentsById,
            DateOnly currentDate)
        {
            foreach (Person resident in residents)
            {
                if (!resident.IsAlive ||
                    resident.MaritalStatus != MaritalStatus.Married ||
                    resident.SpouseId is not
                        { } spouseId ||
                    resident.GetAgeGroup(currentDate) != AgeGroup.Adult)
                    continue;

                if (!residentsById.TryGetValue(
                        key: spouseId,
                        value: out Person? spouse) ||
                    !spouse.IsAlive ||
                    spouse.MaritalStatus != MaritalStatus.Married ||
                    spouse.SpouseId != resident.Id ||
                    spouse.GetAgeGroup(currentDate) != AgeGroup.Adult)
                    continue;

                if (resident.Id.Value.CompareTo(spouse.Id.Value) >= 0)
                    continue;

                yield return (resident, spouse);
            }
        }

        private static Dictionary<int, List<Person>> BuildMarriageLanes(
            IReadOnlyCollection<Person> residents,
            ISet<PersonId> blockedResidents,
            DateOnly currentDate,
            int reviewWindows)
        {
            var lanes = new Dictionary<int, List<Person>>();

            foreach (Person resident in residents)
            {
                if (blockedResidents.Contains(resident.Id) ||
                    !IsEligibleForMarriage(
                        resident: resident,
                        currentDate: currentDate) ||
                    !IsSeekingMarriage(
                        resident: resident,
                        currentDate: currentDate,
                        reviewWindows: reviewWindows))
                    continue;

                int laneKey = ResolveMarriageLane(
                    resident: resident,
                    currentDate: currentDate);

                if (!lanes.TryGetValue(
                        key: laneKey,
                        value: out List<Person>? laneResidents))
                {
                    laneResidents = [];
                    lanes[laneKey] = laneResidents;
                }

                laneResidents.Add(resident);
            }

            return lanes;
        }

        private static bool IsEligibleForMarriage(
            Person resident,
            DateOnly currentDate)
        {
            if (!resident.IsAlive || resident.GetAgeGroup(currentDate) != AgeGroup.Adult)
                return false;

            return resident.MaritalStatus is MaritalStatus.Single or MaritalStatus.Divorced or MaritalStatus.Widowed;
        }

        private static bool IsSeekingMarriage(
            Person resident,
            DateOnly currentDate,
            int reviewWindows)
        {
            double chancePerReview = ResolveMarriageChancePerReview(
                resident: resident,
                currentDate: currentDate);

            return RollOccurs(
                personId: resident.Id,
                currentDate: currentDate,
                salt: 211,
                chancePerReview: chancePerReview,
                reviewWindows: reviewWindows);
        }

        private static bool ShouldDivorce(
            Person first,
            Person second,
            DateOnly currentDate,
            int reviewWindows)
        {
            double chancePerReview = ResolveDivorceChancePerReview(
                first: first,
                second: second);

            return RollOccurs(
                firstResidentId: first.Id,
                secondResidentId: second.Id,
                currentDate: currentDate,
                salt: 307,
                chancePerReview: chancePerReview,
                reviewWindows: reviewWindows);
        }

        private static double ResolveMarriageChancePerReview(
            Person resident,
            DateOnly currentDate)
        {
            double sociability = Normalize(resident.Personality.Sociability);
            double optimism = Normalize(resident.Personality.Optimism);
            double happiness = Normalize(resident.Happiness.Value);
            double health = Normalize(resident.Health.Value);
            double stress = Normalize(resident.Stress.Value);
            double socialNeed = Normalize(resident.SocialNeed.Value);

            int ageYears = resident.GetAge(currentDate)
               .Years;
            double ageFactor = ageYears switch
            {
                <= 21 => 0.004d,
                <= 30 => 0.010d,
                <= 45 => 0.007d,
                <= 55 => 0.004d,
                _ => 0.002d
            };

            double statusFactor = resident.MaritalStatus switch
            {
                MaritalStatus.Divorced => -0.001d,
                MaritalStatus.Widowed => -0.002d,
                _ => 0d
            };

            double studentPenalty = resident.Employment.Status == EmploymentStatus.Student
                ? 0.003d
                : 0d;

            double chance = 0.001d +
                            ageFactor +
                            (sociability * 0.018d) +
                            (socialNeed * 0.020d) +
                            (optimism * 0.008d) +
                            (happiness * 0.006d) +
                            (health * 0.006d) -
                            (stress * 0.012d) +
                            statusFactor -
                            studentPenalty;

            if (resident.Health.Value < 25 || resident.Happiness.Value < 20)
                chance *= 0.45d;

            return Math.Clamp(
                value: chance,
                min: 0.0005d,
                max: 0.050d);
        }

        private static double ResolveDivorceChancePerReview(
            Person first,
            Person second)
        {
            double averageHappiness = Normalize(
                value: first.Happiness.Value + second.Happiness.Value,
                divisor: 200d);
            double averageStress = Normalize(
                value: first.Stress.Value + second.Stress.Value,
                divisor: 200d);
            double averageSocialNeed = Normalize(
                value: first.SocialNeed.Value + second.SocialNeed.Value,
                divisor: 200d);
            double averageOptimism = Normalize(
                value: first.Personality.Optimism + second.Personality.Optimism,
                divisor: 200d);
            double healthBurden = 1d -
                                  Normalize(
                                      value: first.Health.Value + second.Health.Value,
                                      divisor: 200d);

            double chance = 0.0004d +
                            ((1d - averageHappiness) * 0.012d) +
                            (averageStress * 0.010d) +
                            (averageSocialNeed * 0.006d) +
                            (healthBurden * 0.004d) -
                            (averageOptimism * 0.004d);

            if (first.Happiness.Value < 20 || second.Happiness.Value < 20)
                chance += 0.004d;

            return Math.Clamp(
                value: chance,
                min: 0.0002d,
                max: 0.030d);
        }

        private static bool AreMarriageCompatible(
            Person first,
            Person second,
            DateOnly currentDate)
        {
            if (first.Id == second.Id || first.HouseholdId == second.HouseholdId)
                return false;

            int ageGap = Math.Abs(
                first.GetAge(currentDate)
                   .Years -
                second.GetAge(currentDate)
                   .Years);
            if (ageGap > 14)
                return false;

            int sociabilityGap = Math.Abs(first.Personality.Sociability - second.Personality.Sociability);
            int optimismGap = Math.Abs(first.Personality.Optimism - second.Personality.Optimism);
            int disciplineGap = Math.Abs(first.Personality.Discipline - second.Personality.Discipline);

            return sociabilityGap <= 45 &&
                   optimismGap <= 50 &&
                   disciplineGap <= 55;
        }

        private static int CompareMarriageLaneResidents(
            Person left,
            Person right,
            DateOnly currentDate)
        {
            int ageComparison = left.GetAge(currentDate)
               .Years.CompareTo(
                    right.GetAge(currentDate)
                       .Years);
            if (ageComparison != 0)
                return ageComparison;

            int sociabilityComparison = left.Personality.Sociability.CompareTo(right.Personality.Sociability);
            if (sociabilityComparison != 0)
                return sociabilityComparison;

            return left.Id.Value.CompareTo(right.Id.Value);
        }

        private static int ResolveMarriageLane(
            Person resident,
            DateOnly currentDate)
        {
            int ageYears = resident.GetAge(currentDate)
               .Years;
            return Math.Min(
                val1: 5,
                val2: Math.Max(
                    val1: 0,
                    val2: (ageYears - 18) / 12));
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
            PersonId personId,
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
                       personId: personId,
                       currentDate: currentDate,
                       salt: salt) <
                   combinedChance;
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
            return Normalize(
                value: value,
                divisor: 100d);
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

        private static double GetStableFraction(
            PersonId personId,
            DateOnly currentDate,
            int salt)
        {
            return GetStableInt(
                       personId: personId,
                       currentDate: currentDate,
                       salt: salt,
                       modulus: 10_000) /
                   10_000d;
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
                int hash = 23;

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
    }
}
