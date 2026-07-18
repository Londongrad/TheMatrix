using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityEmploymentAutonomyPolicy(
        IPopulationGenerationContentCatalog contentCatalog,
        CityHouseholdEconomyPolicy householdEconomyPolicy,
        CityPopulationAnchorSelectionPolicy anchorSelectionPolicy)
    {
        private readonly IReadOnlyList<PopulationProfessionCatalogItem> _professions =
            contentCatalog.Professions.Count == 0
                ? throw new InvalidOperationException("Population profession catalog must not be empty.")
                : contentCatalog.Professions;

        public bool Apply(
            Person person,
            Household household,
            IReadOnlyCollection<Person> householdResidents,
            IReadOnlyDictionary<PersonId, PersonRoutineProfile> routineProfilesByResidentId,
            IReadOnlyDictionary<PersonId, CityResidentEconomicContext> economicContextsByResidentId,
            DateOnly previousDate,
            DateOnly currentDate,
            HousingStatus? housingStatus,
            DistrictId? preferredDistrictId,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> workplaceAnchors,
            IDictionary<string, List<Job>> workplacePools,
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> employerStressByWorkplaceId,
            IReadOnlyCollection<CityAnchorId>? preferredWorkplaceAnchorIds = null,
            CityPopulationCostOfLivingState? costOfLivingState = null)
        {
            ArgumentNullException.ThrowIfNull(person);
            ArgumentNullException.ThrowIfNull(householdResidents);
            ArgumentNullException.ThrowIfNull(routineProfilesByResidentId);
            ArgumentNullException.ThrowIfNull(economicContextsByResidentId);
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
            CityResidentEconomicContext economicContext = economicContextsByResidentId.GetValueOrDefault(
                person.Id,
                CityResidentEconomicContext.Neutral);
            CityHouseholdEconomyProfile householdEconomy = householdEconomyPolicy.Build(
                household: household,
                householdResidents: householdResidents,
                routineProfilesByResidentId: routineProfilesByResidentId,
                economicContextsByResidentId: economicContextsByResidentId,
                housingStatus: housingStatus,
                currentDate: currentDate,
                costOfLivingState: costOfLivingState);

            return person.Employment.Status switch
            {
                EmploymentStatus.Unemployed or EmploymentStatus.None => TryAssignAutonomousJob(
                    person: person,
                    currentDate: currentDate,
                    reviewWindows: reviewWindows,
                    householdEconomy: householdEconomy,
                    economicContext: economicContext,
                    preferredDistrictId: preferredDistrictId,
                    workplaceAnchors: workplaceAnchors,
                    workplacePools: workplacePools,
                    preferredWorkplaceAnchorIds: preferredWorkplaceAnchorIds,
                    employerStressByWorkplaceId: employerStressByWorkplaceId),
                EmploymentStatus.Employed => TryTriggerJobLoss(
                    person: person,
                    currentDate: currentDate,
                    reviewWindows: reviewWindows,
                    householdEconomy: householdEconomy,
                    employerStressByWorkplaceId: employerStressByWorkplaceId),
                _ => false
            };
        }

        private bool TryAssignAutonomousJob(
            Person person,
            DateOnly currentDate,
            int reviewWindows,
            CityHouseholdEconomyProfile householdEconomy,
            CityResidentEconomicContext economicContext,
            DistrictId? preferredDistrictId,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> workplaceAnchors,
            IDictionary<string, List<Job>> workplacePools,
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> employerStressByWorkplaceId,
            IReadOnlyCollection<CityAnchorId>? preferredWorkplaceAnchorIds)
        {
            PopulationProfessionCatalogItem profession = PickProfession(
                personId: person.Id,
                currentDate: currentDate);
            EmployerMarketAvailability marketAvailability = EvaluateEmployerAvailability(
                jobTitle: profession.Title,
                workplacePools: workplacePools,
                employerStressByWorkplaceId: employerStressByWorkplaceId);
            double chancePerReview = ResolveHireChancePerReview(
                person: person,
                householdEconomy: householdEconomy,
                economicContext: economicContext,
                marketAvailability: marketAvailability);

            if (!RollOccurs(
                    personId: person.Id,
                    currentDate: currentDate,
                    salt: 17,
                    chancePerReview: chancePerReview,
                    reviewWindows: reviewWindows))
                return false;

            Job? job = ResolveJob(
                person: person,
                currentDate: currentDate,
                jobTitle: profession.Title,
                preferredDistrictId: preferredDistrictId,
                workplaceAnchors: workplaceAnchors,
                workplacePools: workplacePools,
                employerStressByWorkplaceId: employerStressByWorkplaceId,
                preferredWorkplaceAnchorIds: preferredWorkplaceAnchorIds);

            if (job is null)
                return false;

            person.AssignJob(
                currentDate: currentDate,
                job: job);

            return true;
        }

        private static bool TryTriggerJobLoss(
            Person person,
            DateOnly currentDate,
            int reviewWindows,
            CityHouseholdEconomyProfile householdEconomy,
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> employerStressByWorkplaceId)
        {
            CityPopulationEmployerFinancialStressState? employerStressState = person.Employment.Job is
            { } job &&
                                                                              employerStressByWorkplaceId.TryGetValue(
                                                                                  key: job.WorkplaceId,
                                                                                  value: out
                                                                                  CityPopulationEmployerFinancialStressState
                                                                                      ? resolvedState)
                ? resolvedState
                : null;
            double chancePerReview = ResolveJobLossChancePerReview(
                person: person,
                householdEconomy: householdEconomy,
                employerStressState: employerStressState);
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

        private Job? ResolveJob(
            Person person,
            DateOnly currentDate,
            string jobTitle,
            DistrictId? preferredDistrictId,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> workplaceAnchors,
            IDictionary<string, List<Job>> workplacePools,
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> employerStressByWorkplaceId,
            IReadOnlyCollection<CityAnchorId>? preferredWorkplaceAnchorIds)
        {
            if (!workplacePools.TryGetValue(
                    key: jobTitle,
                    value: out List<Job>? titlePool))
            {
                titlePool = [];
                workplacePools[jobTitle] = titlePool;
            }

            var openPool = titlePool
               .Where(job => !employerStressByWorkplaceId.TryGetValue(
                                 key: job.WorkplaceId,
                                 value: out CityPopulationEmployerFinancialStressState? stressState) ||
                             !stressState.HasHiringFreeze)
               .ToList();
            List<Job> orderedOpenPool = OrderByPreferredAnchors(
                jobs: openPool,
                preferredWorkplaceAnchorIds: preferredWorkplaceAnchorIds);

            if (titlePool.Count > 0 && openPool.Count == 0)
                return null;

            bool shouldCreateNew = titlePool.Count == 0 ||
                                   (openPool.Count == titlePool.Count &&
                                    titlePool.Count < 12 &&
                                    GetStableFraction(
                                        personId: person.Id,
                                        currentDate: currentDate,
                                        salt: 73) <
                                    0.18d) ||
                                   (titlePool.Count < 12 &&
                                    openPool.Count == 0);

            if (shouldCreateNew)
            {
                CityAnchorId? workplaceAnchorId = anchorSelectionPolicy.SelectWorkplaceAnchor(
                        anchors: workplaceAnchors,
                        preferredDistrictId: preferredDistrictId,
                        stableKey: person.Id.Value,
                        preferredAnchorIds: preferredWorkplaceAnchorIds)
                  ?.CityAnchorId;
                var created = new Job(
                    workplaceId: WorkplaceId.New(),
                    title: jobTitle,
                    workplaceAnchorId: workplaceAnchorId);
                titlePool.Add(created);
                return created;
            }

            int candidateCount = Math.Min(
                val1: orderedOpenPool.Count,
                val2: preferredWorkplaceAnchorIds is not null && preferredWorkplaceAnchorIds.Count > 0
                    ? 3
                    : orderedOpenPool.Count);
            int stableIndex = GetStableInt(
                personId: person.Id,
                currentDate: currentDate,
                salt: 97,
                modulus: candidateCount);

            return orderedOpenPool[stableIndex];
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
            return Math.Clamp(
                value: currentWindow - previousWindow,
                min: 0,
                max: 8);
        }

        private static double ResolveHireChancePerReview(
            Person person,
            CityHouseholdEconomyProfile householdEconomy,
            CityResidentEconomicContext economicContext,
            EmployerMarketAvailability marketAvailability)
        {
            double discipline = Normalize(person.Personality.Discipline);
            double optimism = Normalize(person.Personality.Optimism);
            double health = Normalize(person.Health.Value);
            double energy = Normalize(person.Energy.Value);
            double stress = Normalize(person.Stress.Value);

            double chance = 0.010d +
                            (discipline * 0.030d) +
                            (optimism * 0.015d) +
                            (health * 0.020d) +
                            (energy * 0.020d) -
                            (stress * 0.030d) +
                            economicContext.EmploymentOpportunityBonus;

            chance += householdEconomy.StrainScore * 0.030d;
            chance -= Math.Max(
                          val1: 0d,
                          val2: householdEconomy.EconomicBalance) *
                      0.006d;

            if (person.Health.Value < 25 || person.Energy.Value < 20)
                chance *= 0.40d;

            if (marketAvailability.TotalEmployers > 0)
            {
                if (marketAvailability.OpenEmployerCount <= 0)
                    return 0d;

                chance *= marketAvailability.OpenEmployerRatio switch
                {
                    <= 0.20d => 0.18d,
                    <= 0.40d => 0.35d,
                    <= 0.60d => 0.60d,
                    <= 0.80d => 0.82d,
                    _ => 1.0d
                };
            }

            return Math.Clamp(
                value: chance,
                min: 0.003d,
                max: 0.120d);
        }

        private static double ResolveJobLossChancePerReview(
            Person person,
            CityHouseholdEconomyProfile householdEconomy,
            CityPopulationEmployerFinancialStressState? employerStressState)
        {
            double discipline = Normalize(person.Personality.Discipline);
            double optimism = Normalize(person.Personality.Optimism);
            double stress = Normalize(person.Stress.Value);
            double lowHealth = 1d - Normalize(person.Health.Value);
            double lowEnergy = 1d - Normalize(person.Energy.Value);

            double chance = 0.002d +
                            (stress * 0.020d) +
                            (lowHealth * 0.015d) +
                            (lowEnergy * 0.012d) -
                            (discipline * 0.010d) -
                            (optimism * 0.005d);

            chance -= householdEconomy.StrainScore * 0.010d;
            chance += Math.Max(
                          val1: 0d,
                          val2: householdEconomy.EconomicBalance) *
                      0.004d;

            if (person.Health.Value < 20 || person.Energy.Value < 15 || person.Stress.Value > 90)
                chance += 0.020d;

            if (employerStressState is not null)
            {
                chance += (double)employerStressState.DistressScore * 0.050d;
                chance += (double)(1m - employerStressState.PayrollFulfillmentRatio) * 0.070d;

                if (employerStressState.HasHiringFreeze)
                    chance += 0.010d;

                if (employerStressState.HasLayoffPressure)
                    chance += 0.035d;

                if (employerStressState.PartialPayrollCount > 0)
                    chance += Math.Min(
                        val1: 0.030d,
                        val2: employerStressState.PartialPayrollCount * 0.010d);

                if (employerStressState.FailedPayrollCount > 0)
                    chance += Math.Min(
                        val1: 0.060d,
                        val2: employerStressState.FailedPayrollCount * 0.020d);

                if (employerStressState.MissedGrossPayrollAmount > 0m)
                    chance += Math.Min(
                        val1: 0.030d,
                        val2: (double)(employerStressState.MissedGrossPayrollAmount /
                                       Math.Max(
                                           val1: 1m,
                                           val2: employerStressState.RequestedGrossPayrollAmount)) *
                              0.030d);

                if (employerStressState.CurrentBalanceAmount < 0m)
                    chance += 0.020d;
            }

            return Math.Clamp(
                value: chance,
                min: 0.001d,
                max: 0.090d);
        }

        private static EmployerMarketAvailability EvaluateEmployerAvailability(
            string jobTitle,
            IDictionary<string, List<Job>> workplacePools,
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> employerStressByWorkplaceId)
        {
            if (!workplacePools.TryGetValue(
                    key: jobTitle,
                    value: out List<Job>? titlePool) ||
                titlePool.Count == 0)
                return EmployerMarketAvailability.Empty;

            int openEmployerCount = titlePool.Count(job =>
                !employerStressByWorkplaceId.TryGetValue(
                    key: job.WorkplaceId,
                    value: out CityPopulationEmployerFinancialStressState? stressState) ||
                (!stressState.HasHiringFreeze &&
                 stressState.PayrollFulfillmentRatio >= 0.85m &&
                 stressState.FailedPayrollCount == 0));

            return new EmployerMarketAvailability(
                TotalEmployers: titlePool.Count,
                OpenEmployerCount: openEmployerCount);
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

        private static double Normalize(int value)
        {
            return Math.Clamp(
                value: value / 100d,
                min: 0d,
                max: 1d);
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
                       modulus: 10_000) /
                   10_000d;
        }

        private static List<Job> OrderByPreferredAnchors(
            IReadOnlyCollection<Job> jobs,
            IReadOnlyCollection<CityAnchorId>? preferredWorkplaceAnchorIds)
        {
            var orderedJobs = jobs.ToList();
            if (preferredWorkplaceAnchorIds is null || preferredWorkplaceAnchorIds.Count == 0)
                return orderedJobs;

            var preferredOrder = preferredWorkplaceAnchorIds
               .Select((
                    anchorId,
                    index) => new
                    {
                        anchorId,
                        index
                    })
               .ToDictionary(
                    keySelector: x => x.anchorId,
                    elementSelector: x => x.index);

            return orderedJobs
               .OrderBy(x => x.WorkplaceAnchorId is not null && preferredOrder.ContainsKey(x.WorkplaceAnchorId.Value)
                    ? 0
                    : 1)
               .ThenBy(x => x.WorkplaceAnchorId is not null &&
                            preferredOrder.TryGetValue(
                                key: x.WorkplaceAnchorId.Value,
                                value: out int order)
                    ? order
                    : int.MaxValue)
               .ToList();
        }

        private readonly record struct EmployerMarketAvailability(
            int TotalEmployers,
            int OpenEmployerCount)
        {
            public static EmployerMarketAvailability Empty => new(
                TotalEmployers: 0,
                OpenEmployerCount: 0);

            public double OpenEmployerRatio => TotalEmployers <= 0
                ? 1d
                : OpenEmployerCount / (double)TotalEmployers;
        }
    }
}
