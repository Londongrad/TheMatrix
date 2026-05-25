using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdIndependenceAutonomyPolicy(CityHouseholdLivelihoodPolicy householdLivelihoodPolicy)
    {
        public IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> Plan(
            IReadOnlyCollection<Person> residents,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingStatuses,
            DateOnly previousDate,
            DateOnly currentDate)
        {
            ArgumentNullException.ThrowIfNull(residents);
            ArgumentNullException.ThrowIfNull(housingStatuses);

            if (currentDate <= previousDate || residents.Count == 0)
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
                    elementSelector: x => x.ToList());

            var decisions = new List<CityHouseholdIndependenceAutonomyDecision>();

            foreach ((HouseholdId householdId, List<Person> members) in householdResidents)
            {
                if (members.Count <= 1 ||
                    !housingStatuses.TryGetValue(
                        key: householdId,
                        value: out HousingStatus housingStatus) ||
                    housingStatus != HousingStatus.Housed)
                    continue;

                HouseholdIndependenceProfile profile = BuildProfile(
                    householdId: householdId,
                    members: members,
                    housingStatus: housingStatus,
                    residentsById: residentsById,
                    currentDate: currentDate);

                Person? candidate = ResolveCandidate(profile);
                if (candidate is null)
                    continue;

                if (!ShouldMoveOut(
                        resident: candidate,
                        profile: profile,
                        currentDate: currentDate,
                        reviewWindows: reviewWindows))
                    continue;

                decisions.Add(
                    new CityHouseholdIndependenceAutonomyDecision(
                        ResidentId: candidate.Id,
                        SourceHouseholdId: householdId));
            }

            return decisions;
        }

        private HouseholdIndependenceProfile BuildProfile(
            HouseholdId householdId,
            IReadOnlyCollection<Person> members,
            HousingStatus housingStatus,
            IReadOnlyDictionary<PersonId, Person> residentsById,
            DateOnly currentDate)
        {
            bool hasInfant = false;
            int childCount = 0;
            int employedAdults = 0;
            int activeIllnessCount = 0;
            double averageStress = 0d;
            double averageHappiness = 0d;

            foreach (Person member in members)
            {
                if (member.GetAge(currentDate)
                       .Years ==
                    0)
                    hasInfant = true;

                if (member.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Youth)
                    childCount++;

                if (member.GetAgeGroup(currentDate) is AgeGroup.Adult or AgeGroup.Senior &&
                    member.Employment.Status == EmploymentStatus.Employed)
                    employedAdults++;

                if (member.HasActiveIllness)
                    activeIllnessCount++;

                averageStress += member.Stress.Value;
                averageHappiness += member.Happiness.Value;
            }

            averageStress /= members.Count;
            averageHappiness /= members.Count;

            HouseholdIndependenceCandidate[] candidates = members
               .Where(x => IsEligibleForIndependence(
                    resident: x,
                    currentDate: currentDate))
               .Where(x => !HasChildInSameHousehold(
                    resident: x,
                    householdResidents: members))
               .Select(x => new HouseholdIndependenceCandidate(
                    Resident: x,
                    LivesWithParent: LivesWithParent(
                        resident: x,
                        householdResidents: members,
                        residentsById: residentsById)))
               .ToArray();

            CityHouseholdLivelihoodProfile livelihoodProfile = householdLivelihoodPolicy.Build(
                householdResidents: members,
                housingStatus: housingStatus,
                currentDate: currentDate);

            return new HouseholdIndependenceProfile(
                HouseholdId: householdId,
                Size: members.Count,
                ChildCount: childCount,
                EmployedAdults: employedAdults,
                ActiveIllnessCount: activeIllnessCount,
                HasInfant: hasInfant,
                AverageStress: averageStress,
                AverageHappiness: averageHappiness,
                LivelihoodProfile: livelihoodProfile,
                Candidates: candidates);
        }

        private static Person? ResolveCandidate(HouseholdIndependenceProfile profile)
        {
            HouseholdIndependenceCandidate? candidate = profile.Candidates
               .OrderByDescending(x => x.LivesWithParent)
               .ThenByDescending(x => x.Resident.Employment.Status == EmploymentStatus.Employed)
               .ThenByDescending(x => x.Resident.Stress.Value)
               .ThenBy(x => x.Resident.Id.Value)
               .FirstOrDefault();

            return candidate?.Resident;
        }

        private bool ShouldMoveOut(
            Person resident,
            HouseholdIndependenceProfile profile,
            DateOnly currentDate,
            int reviewWindows)
        {
            HouseholdIndependenceCandidate candidate = profile.Candidates.First(x => x.Resident.Id == resident.Id);

            double employmentStrength = resident.Employment.Status == EmploymentStatus.Employed
                ? 0.030d
                : resident.Employment.Status == EmploymentStatus.Student
                    ? 0.006d
                    : -0.006d;

            double ageFactor = resident.GetAge(currentDate)
                   .Years switch
            {
                <= 21 => 0.010d,
                <= 27 => 0.024d,
                <= 34 => 0.018d,
                <= 42 => 0.010d,
                _ => 0.004d
            };

            double stress = Normalize(resident.Stress.Value);
            double lowHappiness = 1d - Normalize(resident.Happiness.Value);
            double lowHealth = 1d - Normalize(resident.Health.Value);
            double householdCrowding = Math.Clamp(
                value: (profile.Size - 2) / 4d,
                min: 0d,
                max: 1d);
            double householdStress = Normalize(profile.AverageStress);
            double householdLowHappiness = 1d - Normalize(profile.AverageHappiness);
            double selfReliance = householdLivelihoodPolicy.ResolveResidentSelfReliance(resident);
            double launchReadiness = Math.Clamp(
                value: (profile.LivelihoodProfile.StabilityScore * 0.40d) +
                       (selfReliance * 0.60d),
                min: 0d,
                max: 1d);

            double chance = 0.001d +
                            ageFactor +
                            employmentStrength +
                            (candidate.LivesWithParent
                                ? 0.018d
                                : 0d) +
                            (householdCrowding * 0.024d) +
                            (stress * 0.018d) +
                            (lowHappiness * 0.012d) +
                            (householdStress * 0.010d) +
                            (householdLowHappiness * 0.008d) -
                            (lowHealth * 0.010d) -
                            (profile.HasInfant
                                ? 0.008d
                                : 0d) -
                            (profile.ActiveIllnessCount > 0
                                ? 0.006d
                                : 0d);

            chance *= Math.Clamp(
                value: 0.30d + launchReadiness,
                min: 0.25d,
                max: 1.10d);

            if (selfReliance < 0.25d)
                chance *= 0.45d;

            if (profile.ChildCount == 0 && profile.Size <= 2)
                chance *= 0.40d;

            return RollOccurs(
                residentId: resident.Id,
                currentDate: currentDate,
                salt: 1_277,
                chancePerReview: Math.Clamp(
                    value: chance,
                    min: 0.0005d,
                    max: 0.100d),
                reviewWindows: reviewWindows);
        }

        private static bool IsEligibleForIndependence(
            Person resident,
            DateOnly currentDate)
        {
            if (!resident.IsAlive || resident.GetAgeGroup(currentDate) != AgeGroup.Adult)
                return false;

            if (resident.MaritalStatus is not (MaritalStatus.Single or MaritalStatus.Divorced or MaritalStatus.Widowed))
                return false;

            return resident.SpouseId is null;
        }

        private static bool HasChildInSameHousehold(
            Person resident,
            IReadOnlyCollection<Person> householdResidents)
        {
            return householdResidents.Any(x => x.MotherId == resident.Id || x.FatherId == resident.Id);
        }

        private static bool LivesWithParent(
            Person resident,
            IReadOnlyCollection<Person> householdResidents,
            IReadOnlyDictionary<PersonId, Person> residentsById)
        {
            if (resident.MotherId is
                { } motherId &&
                residentsById.TryGetValue(
                    key: motherId,
                    value: out Person? mother) &&
                mother.HouseholdId == resident.HouseholdId &&
                mother.IsAlive &&
                householdResidents.Any(x => x.Id == motherId))
                return true;

            if (resident.FatherId is
                { } fatherId &&
                residentsById.TryGetValue(
                    key: fatherId,
                    value: out Person? father) &&
                father.HouseholdId == resident.HouseholdId &&
                father.IsAlive &&
                householdResidents.Any(x => x.Id == fatherId))
                return true;

            return false;
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
            PersonId residentId,
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
                       residentId: residentId,
                       currentDate: currentDate,
                       salt: salt) <
                   combinedChance;
        }

        private static double Normalize(double value)
        {
            return Math.Clamp(
                value: value / 100d,
                min: 0d,
                max: 1d);
        }

        private static int GetStableInt(
            PersonId residentId,
            DateOnly currentDate,
            int salt,
            int modulus)
        {
            if (modulus <= 0)
                return 0;

            unchecked
            {
                byte[] bytes = residentId.Value.ToByteArray();
                int hash = 17;
                for (int i = 0; i < bytes.Length; i++)
                    hash = (hash * 31) + bytes[i];

                hash = (hash * 31) + currentDate.DayNumber;
                hash = (hash * 31) + salt;

                return (int)(Math.Abs((long)hash) % modulus);
            }
        }

        private static double GetStableFraction(
            PersonId residentId,
            DateOnly currentDate,
            int salt)
        {
            return GetStableInt(
                       residentId: residentId,
                       currentDate: currentDate,
                       salt: salt,
                       modulus: 10_000) /
                   10_000d;
        }

        private sealed record HouseholdIndependenceProfile(
            HouseholdId HouseholdId,
            int Size,
            int ChildCount,
            int EmployedAdults,
            int ActiveIllnessCount,
            bool HasInfant,
            double AverageStress,
            double AverageHappiness,
            CityHouseholdLivelihoodProfile LivelihoodProfile,
            IReadOnlyCollection<HouseholdIndependenceCandidate> Candidates);

        private sealed record HouseholdIndependenceCandidate(
            Person Resident,
            bool LivesWithParent);
    }
}
