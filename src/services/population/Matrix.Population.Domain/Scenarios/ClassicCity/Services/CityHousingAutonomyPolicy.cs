using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityHousingAutonomyPolicy(CityHouseholdEconomyPolicy householdEconomyPolicy)
    {
        public IReadOnlyList<CityHousingAutonomyDecision> Plan(
            IReadOnlyDictionary<HouseholdId, Household> households,
            IReadOnlyCollection<Person> residents,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingStatuses,
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> financialStressStates,
            DateOnly previousDate,
            DateOnly currentDate,
            CityPopulationCostOfLivingState? costOfLivingState = null,
            CityPopulationServiceQualityState? serviceQualityState = null)
        {
            ArgumentNullException.ThrowIfNull(households);
            ArgumentNullException.ThrowIfNull(residents);
            ArgumentNullException.ThrowIfNull(housingStatuses);
            ArgumentNullException.ThrowIfNull(financialStressStates);

            if (currentDate <= previousDate ||
                households.Count == 0 ||
                residents.Count == 0 ||
                housingStatuses.Count == 0)
                return [];

            int reviewWindows = ResolveMonthlyReviewWindows(
                previousDate: previousDate,
                currentDate: currentDate);

            if (reviewWindows <= 0)
                return [];

            var householdResidents = residents
               .Where(x => x.IsAlive)
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.ToList());

            var decisions = new List<CityHousingAutonomyDecision>();

            foreach ((HouseholdId householdId, HousingStatus housingStatus) in housingStatuses)
            {
                if (!households.TryGetValue(
                        key: householdId,
                        value: out Household? household))
                    continue;

                if (!householdResidents.TryGetValue(
                        key: householdId,
                        value: out List<Person>? members) ||
                    members.Count == 0)
                    continue;

                HouseholdHousingProfile profile = BuildProfile(
                    householdId: householdId,
                    members: members,
                    currentDate: currentDate);
                financialStressStates.TryGetValue(
                    key: householdId,
                    value: out CityPopulationHouseholdFinancialStressState? financialStressState);
                CityHouseholdEconomyProfile economyProfile = householdEconomyPolicy.Build(
                    household: household,
                    householdResidents: members,
                    housingStatus: housingStatus,
                    currentDate: currentDate,
                    costOfLivingState: costOfLivingState);
                decimal housingSupportIndex = serviceQualityState?.HousingSupportIndex ?? 1m;

                if (housingStatus == HousingStatus.Housed &&
                    ShouldForceLoseHousing(
                        profile: profile,
                        economyProfile: economyProfile,
                        financialStressState: financialStressState,
                        currentDate: currentDate,
                        housingSupportIndex: housingSupportIndex))
                {
                    decisions.Add(
                        new CityHousingAutonomyDecision(
                            Type: CityHousingAutonomyDecisionType.LoseHousing,
                            HouseholdId: householdId));
                    continue;
                }

                switch (housingStatus)
                {
                    case HousingStatus.Homeless when ShouldFindHousing(
                        profile: profile,
                        economyProfile: economyProfile,
                        financialStressState: financialStressState,
                        currentDate: currentDate,
                        reviewWindows: reviewWindows,
                        housingSupportIndex: housingSupportIndex):
                        decisions.Add(
                            new CityHousingAutonomyDecision(
                                Type: CityHousingAutonomyDecisionType.FindHousing,
                                HouseholdId: householdId));
                        break;

                    case HousingStatus.Housed when ShouldLoseHousing(
                        profile: profile,
                        economyProfile: economyProfile,
                        financialStressState: financialStressState,
                        currentDate: currentDate,
                        reviewWindows: reviewWindows,
                        housingSupportIndex: housingSupportIndex):
                        decisions.Add(
                            new CityHousingAutonomyDecision(
                                Type: CityHousingAutonomyDecisionType.LoseHousing,
                                HouseholdId: householdId));
                        break;
                }
            }

            return decisions;
        }

        private static HouseholdHousingProfile BuildProfile(
            HouseholdId householdId,
            IReadOnlyCollection<Person> members,
            DateOnly currentDate)
        {
            int size = members.Count;
            int adultCount = 0;
            int seniorCount = 0;
            int childCount = 0;
            int employedAdults = 0;
            int studentResidents = 0;
            int activeIllnessCount = 0;
            bool hasInfant = false;
            double healthTotal = 0d;
            double happinessTotal = 0d;
            double energyTotal = 0d;
            double stressTotal = 0d;
            double socialNeedTotal = 0d;

            foreach (Person member in members)
            {
                AgeGroup ageGroup = member.GetAgeGroup(currentDate);
                switch (ageGroup)
                {
                    case AgeGroup.Adult:
                        adultCount++;
                        break;
                    case AgeGroup.Senior:
                        seniorCount++;
                        break;
                    case AgeGroup.Child:
                    case AgeGroup.Youth:
                        childCount++;
                        break;
                }

                if (ageGroup is AgeGroup.Adult or AgeGroup.Senior &&
                    member.Employment.Status == EmploymentStatus.Employed)
                    employedAdults++;

                if (member.Employment.Status == EmploymentStatus.Student)
                    studentResidents++;

                if (member.HasActiveIllness)
                    activeIllnessCount++;

                if (member.GetAge(currentDate)
                       .Years ==
                    0)
                    hasInfant = true;

                healthTotal += member.Health.Value;
                happinessTotal += member.Happiness.Value;
                energyTotal += member.Energy.Value;
                stressTotal += member.Stress.Value;
                socialNeedTotal += member.SocialNeed.Value;
            }

            return new HouseholdHousingProfile(
                HouseholdId: householdId,
                Size: size,
                AdultCount: adultCount,
                SeniorCount: seniorCount,
                ChildCount: childCount,
                EmployedAdults: employedAdults,
                StudentResidents: studentResidents,
                ActiveIllnessCount: activeIllnessCount,
                HasInfant: hasInfant,
                AverageHealth: healthTotal / size,
                AverageHappiness: happinessTotal / size,
                AverageEnergy: energyTotal / size,
                AverageStress: stressTotal / size,
                AverageSocialNeed: socialNeedTotal / size);
        }

        private static bool ShouldFindHousing(
            HouseholdHousingProfile profile,
            CityHouseholdEconomyProfile economyProfile,
            CityPopulationHouseholdFinancialStressState? financialStressState,
            DateOnly currentDate,
            int reviewWindows,
            decimal housingSupportIndex)
        {
            if (profile.AdultCount + profile.SeniorCount <= 0)
                return false;

            double chancePerReview = ResolveFindHousingChancePerReview(
                profile: profile,
                economyProfile: economyProfile,
                financialStressState: financialStressState,
                currentDate: currentDate,
                housingSupportIndex: housingSupportIndex);
            return RollOccurs(
                householdId: profile.HouseholdId,
                currentDate: currentDate,
                salt: 811,
                chancePerReview: chancePerReview,
                reviewWindows: reviewWindows);
        }

        private static bool ShouldLoseHousing(
            HouseholdHousingProfile profile,
            CityHouseholdEconomyProfile economyProfile,
            CityPopulationHouseholdFinancialStressState? financialStressState,
            DateOnly currentDate,
            int reviewWindows,
            decimal housingSupportIndex)
        {
            double chancePerReview = ResolveLoseHousingChancePerReview(
                profile: profile,
                economyProfile: economyProfile,
                financialStressState: financialStressState,
                currentDate: currentDate,
                housingSupportIndex: housingSupportIndex);
            return RollOccurs(
                householdId: profile.HouseholdId,
                currentDate: currentDate,
                salt: 907,
                chancePerReview: chancePerReview,
                reviewWindows: reviewWindows);
        }

        private static bool ShouldForceLoseHousing(
            HouseholdHousingProfile profile,
            CityHouseholdEconomyProfile economyProfile,
            CityPopulationHouseholdFinancialStressState? financialStressState,
            DateOnly currentDate,
            decimal housingSupportIndex)
        {
            if (!IsRecentFinancialStress(
                    state: financialStressState,
                    currentDate: currentDate))
                return false;

            if (financialStressState!.EvictionEligibleCount <= 0)
                return false;

            if (financialStressState.OverdueRentCount <= 0 &&
                financialStressState.TotalOverdueAmount <= 0m)
                return false;

            if (financialStressState.DistressScore < 0.58m &&
                financialStressState.OldestOverdueAgeDays < 60)
                return false;

            if (economyProfile.StrainScore < 0.72d &&
                financialStressState.OldestOverdueAgeDays < 75)
                return false;

            if (economyProfile.CashReserveAmount > 0m &&
                economyProfile.DailyNetAmount >= 0m &&
                financialStressState.OldestOverdueAgeDays < 90)
                return false;

            if (profile.HasInfant &&
                financialStressState.DistressScore < 0.80m &&
                financialStressState.OldestOverdueAgeDays < 90)
                return false;

            if (housingSupportIndex >= 1.10m &&
                (profile.HasInfant || profile.ChildCount > 0) &&
                financialStressState.OldestOverdueAgeDays < 120)
                return false;

            return true;
        }

        private static double ResolveFindHousingChancePerReview(
            HouseholdHousingProfile profile,
            CityHouseholdEconomyProfile economyProfile,
            CityPopulationHouseholdFinancialStressState? financialStressState,
            DateOnly currentDate,
            decimal housingSupportIndex)
        {
            double averageHealth = Normalize(profile.AverageHealth);
            double averageHappiness = Normalize(profile.AverageHappiness);
            double averageEnergy = Normalize(profile.AverageEnergy);
            double averageStress = Normalize(profile.AverageStress);
            double averageSocialNeed = Normalize(profile.AverageSocialNeed);
            double employmentStrength = profile.AdultCount + profile.SeniorCount > 0
                ? profile.EmployedAdults / (double)(profile.AdultCount + profile.SeniorCount)
                : 0d;
            double illnessBurden = profile.Size > 0
                ? profile.ActiveIllnessCount / (double)profile.Size
                : 0d;

            double chance = 0.003d +
                            (employmentStrength * 0.040d) +
                            (averageHealth * 0.014d) +
                            (averageHappiness * 0.012d) +
                            (averageEnergy * 0.010d) +
                            (averageSocialNeed * 0.008d) +
                            (profile.ChildCount * 0.004d) +
                            (profile.StudentResidents * 0.003d) +
                            (profile.HasInfant
                                ? 0.008d
                                : 0d) -
                            (averageStress * 0.014d) -
                            (illnessBurden * 0.012d) -
                            (Math.Max(
                                 val1: 0,
                                 val2: profile.Size - 3) *
                             0.003d);

            double financialStressScore = ResolveRecentFinancialStressScore(
                state: financialStressState,
                currentDate: currentDate);
            int overdueRentCount = ResolveRecentOverdueRentCount(
                state: financialStressState,
                currentDate: currentDate);
            int overdueUtilityCount = ResolveRecentOverdueUtilityCount(
                state: financialStressState,
                currentDate: currentDate);
            int arrearsObligationCount = ResolveRecentArrearsObligationCount(
                state: financialStressState,
                currentDate: currentDate);
            int serviceCutoffCount = ResolveRecentServiceCutoffCount(
                state: financialStressState,
                currentDate: currentDate);
            int evictionNoticeCount = ResolveRecentEvictionNoticeCount(
                state: financialStressState,
                currentDate: currentDate);
            int evictionEligibleCount = ResolveRecentEvictionEligibleCount(
                state: financialStressState,
                currentDate: currentDate);
            int oldestOverdueAgeDays = ResolveRecentOldestOverdueAgeDays(
                state: financialStressState,
                currentDate: currentDate);
            decimal overdueAmount = ResolveRecentOverdueAmount(
                state: financialStressState,
                currentDate: currentDate);

            chance += Math.Max(
                          val1: 0d,
                          val2: economyProfile.EconomicBalance) *
                      0.018d;
            chance += economyProfile.GrowthReadinessScore * 0.012d;
            chance += (double)((housingSupportIndex - 1m) * 0.040m);
            chance -= economyProfile.StrainScore * 0.010d;
            chance -= financialStressScore * 0.026d;
            chance -= Math.Min(
                val1: 0.018d,
                val2: overdueRentCount * 0.008d);
            chance -= Math.Min(
                val1: 0.010d,
                val2: overdueUtilityCount * 0.004d);
            chance -= Math.Min(
                val1: 0.012d,
                val2: arrearsObligationCount * 0.004d);
            chance -= Math.Min(
                val1: 0.016d,
                val2: serviceCutoffCount * 0.008d);
            chance -= Math.Min(
                val1: 0.028d,
                val2: evictionNoticeCount * 0.014d);
            chance -= Math.Min(
                val1: 0.050d,
                val2: evictionEligibleCount * 0.025d);
            chance -= Math.Min(
                val1: 0.020d,
                val2: oldestOverdueAgeDays / 240d);
            chance -= Math.Min(
                val1: 0.018d,
                val2: (double)(overdueAmount / 2_500m));

            if (profile.EmployedAdults == 0 && profile.StudentResidents == 0)
                chance *= 0.45d;

            return Math.Clamp(
                value: chance,
                min: 0.001d,
                max: 0.120d);
        }

        private static double ResolveLoseHousingChancePerReview(
            HouseholdHousingProfile profile,
            CityHouseholdEconomyProfile economyProfile,
            CityPopulationHouseholdFinancialStressState? financialStressState,
            DateOnly currentDate,
            decimal housingSupportIndex)
        {
            double lowHealth = 1d - Normalize(profile.AverageHealth);
            double lowHappiness = 1d - Normalize(profile.AverageHappiness);
            double lowEnergy = 1d - Normalize(profile.AverageEnergy);
            double stress = Normalize(profile.AverageStress);
            double illnessBurden = profile.Size > 0
                ? profile.ActiveIllnessCount / (double)profile.Size
                : 0d;
            double unemploymentBurden = profile.AdultCount + profile.SeniorCount > 0
                ? (profile.AdultCount + profile.SeniorCount - profile.EmployedAdults) /
                  (double)(profile.AdultCount + profile.SeniorCount)
                : 1d;

            double chance = 0.0004d +
                            (unemploymentBurden * 0.018d) +
                            (stress * 0.014d) +
                            (lowHealth * 0.012d) +
                            (lowHappiness * 0.010d) +
                            (lowEnergy * 0.008d) +
                            (illnessBurden * 0.010d) +
                            (Math.Max(
                                 val1: 0,
                                 val2: profile.Size - 4) *
                             0.004d) -
                            (profile.ChildCount * 0.003d) -
                            (profile.EmployedAdults * 0.004d);

            double financialStressScore = ResolveRecentFinancialStressScore(
                state: financialStressState,
                currentDate: currentDate);
            int overdueRentCount = ResolveRecentOverdueRentCount(
                state: financialStressState,
                currentDate: currentDate);
            int overdueUtilityCount = ResolveRecentOverdueUtilityCount(
                state: financialStressState,
                currentDate: currentDate);
            int arrearsObligationCount = ResolveRecentArrearsObligationCount(
                state: financialStressState,
                currentDate: currentDate);
            int serviceCutoffCount = ResolveRecentServiceCutoffCount(
                state: financialStressState,
                currentDate: currentDate);
            int evictionNoticeCount = ResolveRecentEvictionNoticeCount(
                state: financialStressState,
                currentDate: currentDate);
            int evictionEligibleCount = ResolveRecentEvictionEligibleCount(
                state: financialStressState,
                currentDate: currentDate);
            int oldestOverdueAgeDays = ResolveRecentOldestOverdueAgeDays(
                state: financialStressState,
                currentDate: currentDate);
            decimal overdueAmount = ResolveRecentOverdueAmount(
                state: financialStressState,
                currentDate: currentDate);

            chance += economyProfile.StrainScore * 0.016d;
            chance -= (double)((housingSupportIndex - 1m) * 0.050m);
            chance -= Math.Max(
                          val1: 0d,
                          val2: economyProfile.EconomicBalance) *
                      0.010d;
            chance += financialStressScore * 0.034d;
            chance += Math.Min(
                val1: 0.020d,
                val2: overdueRentCount * 0.010d);
            chance += Math.Min(
                val1: 0.012d,
                val2: overdueUtilityCount * 0.005d);
            chance += Math.Min(
                val1: 0.012d,
                val2: arrearsObligationCount * 0.005d);
            chance += Math.Min(
                val1: 0.018d,
                val2: serviceCutoffCount * 0.009d);
            chance += Math.Min(
                val1: 0.028d,
                val2: evictionNoticeCount * 0.014d);
            chance += Math.Min(
                val1: 0.050d,
                val2: evictionEligibleCount * 0.025d);
            chance += Math.Min(
                val1: 0.022d,
                val2: oldestOverdueAgeDays / 180d);
            chance += Math.Min(
                val1: 0.018d,
                val2: (double)(overdueAmount / 1_800m));

            if (profile.HasInfant)
                chance *= 0.60d;

            return Math.Clamp(
                value: chance,
                min: 0.0002d,
                max: 0.050d);
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
            HouseholdId householdId,
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
                       householdId: householdId,
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

        private static double ResolveRecentFinancialStressScore(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            return !IsRecentFinancialStress(
                state: state,
                currentDate: currentDate)
                ? 0d
                : (double)state!.DistressScore;
        }

        private static int ResolveRecentOverdueRentCount(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            return IsRecentFinancialStress(
                state: state,
                currentDate: currentDate)
                ? state!.OverdueRentCount
                : 0;
        }

        private static int ResolveRecentOverdueUtilityCount(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            return IsRecentFinancialStress(
                state: state,
                currentDate: currentDate)
                ? state!.OverdueUtilityCount
                : 0;
        }

        private static decimal ResolveRecentOverdueAmount(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            return IsRecentFinancialStress(
                state: state,
                currentDate: currentDate)
                ? state!.TotalOverdueAmount
                : 0m;
        }

        private static int ResolveRecentArrearsObligationCount(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            return IsRecentFinancialStress(
                state: state,
                currentDate: currentDate)
                ? state!.ArrearsObligationCount
                : 0;
        }

        private static int ResolveRecentServiceCutoffCount(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            return IsRecentFinancialStress(
                state: state,
                currentDate: currentDate)
                ? state!.ServiceCutoffCount
                : 0;
        }

        private static int ResolveRecentEvictionNoticeCount(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            return IsRecentFinancialStress(
                state: state,
                currentDate: currentDate)
                ? state!.EvictionNoticeCount
                : 0;
        }

        private static int ResolveRecentEvictionEligibleCount(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            return IsRecentFinancialStress(
                state: state,
                currentDate: currentDate)
                ? state!.EvictionEligibleCount
                : 0;
        }

        private static int ResolveRecentOldestOverdueAgeDays(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            return IsRecentFinancialStress(
                state: state,
                currentDate: currentDate)
                ? state!.OldestOverdueAgeDays
                : 0;
        }

        private static bool IsRecentFinancialStress(
            CityPopulationHouseholdFinancialStressState? state,
            DateOnly currentDate)
        {
            if (state is null)
                return false;

            var lastEvaluatedDate = DateOnly.FromDateTime(state.LastEvaluatedAtUtc.UtcDateTime);
            return currentDate.DayNumber - lastEvaluatedDate.DayNumber <= 45;
        }

        private static int GetStableInt(
            HouseholdId householdId,
            DateOnly currentDate,
            int salt,
            int modulus)
        {
            if (modulus <= 0)
                return 0;

            unchecked
            {
                byte[] bytes = householdId.Value.ToByteArray();
                int hash = 17;
                for (int i = 0; i < bytes.Length; i++)
                    hash = (hash * 31) + bytes[i];

                hash = (hash * 31) + currentDate.DayNumber;
                hash = (hash * 31) + salt;

                return (int)(Math.Abs((long)hash) % modulus);
            }
        }

        private static double GetStableFraction(
            HouseholdId householdId,
            DateOnly currentDate,
            int salt)
        {
            return GetStableInt(
                       householdId: householdId,
                       currentDate: currentDate,
                       salt: salt,
                       modulus: 10_000) /
                   10_000d;
        }

        private sealed record HouseholdHousingProfile(
            HouseholdId HouseholdId,
            int Size,
            int AdultCount,
            int SeniorCount,
            int ChildCount,
            int EmployedAdults,
            int StudentResidents,
            int ActiveIllnessCount,
            bool HasInfant,
            double AverageHealth,
            double AverageHappiness,
            double AverageEnergy,
            double AverageStress,
            double AverageSocialNeed);
    }
}
