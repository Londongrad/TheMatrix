using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Common
{
    internal static class ClassicCityHousingOpportunityPlanner
    {
        public static CityDistrictUtilityConditionsSnapshot? ResolveDistrictUtilityConditions(
            DistrictId? districtId,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId)
        {
            if (!districtId.HasValue)
                return null;

            return districtUtilityConditionsByDistrictId.TryGetValue(
                key: districtId.Value,
                value: out CityDistrictUtilityConditionsSnapshot? snapshot)
                ? snapshot
                : null;
        }

        public static List<(DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> BuildHousingOpportunityPool(
            IEnumerable<ClassicCityHouseholdPlacement> placements)
        {
            return placements
               .Where(x => x.HousingStatus == HousingStatus.Housed &&
                           x.DistrictId.HasValue &&
                           x.ResidentialBuildingId.HasValue)
               .Select(x => (x.DistrictId!.Value, x.ResidentialBuildingId!.Value))
               .Distinct()
               .ToList();
        }

        public static Person SelectHousingAnchorResident(
            IReadOnlyCollection<Person> householdResidents,
            DateOnly currentDate)
        {
            return householdResidents
               .OrderByDescending(x => x.GetAgeGroup(currentDate) is AgeGroup.Adult or AgeGroup.Senior)
               .ThenByDescending(x => x.GetAge(currentDate)
                   .Years)
               .ThenBy(x => x.Id.Value)
               .First();
        }

        public static async Task<(DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> SelectHousingOpportunityAsync(
            CityId cityId,
            HouseholdId householdId,
            DateOnly currentDate,
            IReadOnlyList<(DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> housingPool,
            IReadOnlyCollection<Person> householdResidents,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            int startIndex = GetStableInt(
                householdId: householdId,
                currentDate: currentDate,
                salt: 1_123,
                modulus: housingPool.Count);
            int candidateCount = Math.Min(
                val1: housingPool.Count,
                val2: 12);
            decimal bestScore = decimal.MinValue;
            int bestIndex = startIndex;
            List<(int CandidateIndex, DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> candidates = [];

            for (int offset = 0; offset < candidateCount; offset++)
            {
                int candidateIndex = (startIndex + offset) % housingPool.Count;
                (DistrictId districtId, ResidentialBuildingId residentialBuildingId) candidate = housingPool[candidateIndex];
                candidates.Add((candidateIndex, candidate.districtId, candidate.residentialBuildingId));
            }

            await commuteRoutingService.PreloadAnchorCommutesAsync(
                cityId: cityId.Value,
                requests: BuildPreloadRequests(
                    candidates: candidates,
                    currentDate: currentDate,
                    householdResidents: householdResidents,
                    hospitalAnchors: hospitalAnchors,
                    anchorSelectionPolicy: anchorSelectionPolicy),
                cancellationToken: cancellationToken);

            foreach ((int candidateIndex, DistrictId districtId, ResidentialBuildingId residentialBuildingId) in candidates)
            {
                decimal candidateScore = await EvaluateHousingOpportunityScoreAsync(
                    cityId: cityId,
                    districtId: districtId,
                    residentialBuildingId: residentialBuildingId,
                    currentDate: currentDate,
                    householdResidents: householdResidents,
                    hospitalAnchors: hospitalAnchors,
                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                    anchorSelectionPolicy: anchorSelectionPolicy,
                    commuteRoutingService: commuteRoutingService,
                    cancellationToken: cancellationToken);

                if (candidateScore > bestScore)
                {
                    bestScore = candidateScore;
                    bestIndex = candidateIndex;
                }
            }

            return housingPool[bestIndex];
        }

        private static IReadOnlyCollection<CityPopulationCommuteRouteRequest> BuildPreloadRequests(
            IReadOnlyCollection<(int CandidateIndex, DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> candidates,
            DateOnly currentDate,
            IReadOnlyCollection<Person> householdResidents,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy)
        {
            List<CityPopulationCommuteRouteRequest> requests = [];

            foreach ((_, DistrictId districtId, ResidentialBuildingId residentialBuildingId) in candidates)
            {
                foreach (Person resident in householdResidents)
                {
                    if (!resident.IsAlive)
                        continue;

                    if (resident.Employment.Job?.WorkplaceAnchorId is { } workplaceAnchorId)
                        requests.Add(
                            new CityPopulationCommuteRouteRequest(
                                ResidentialBuildingId: residentialBuildingId,
                                DestinationAnchorId: workplaceAnchorId,
                                Profile: CityPopulationCommuteRoutingProfiles.Pedestrian));

                    if (resident.Education.CurrentInstitutionAnchorId is { } institutionAnchorId)
                        requests.Add(
                            new CityPopulationCommuteRouteRequest(
                                ResidentialBuildingId: residentialBuildingId,
                                DestinationAnchorId: institutionAnchorId,
                                Profile: CityPopulationCommuteRoutingProfiles.Pedestrian));

                    bool needsHealthcarePriority = resident.HasActiveIllness ||
                                                   resident.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior;
                    if (!needsHealthcarePriority)
                        continue;

                    CityPopulationAnchorCatalogItem? primaryCareAnchor = anchorSelectionPolicy.SelectHospitalAnchor(
                        anchors: hospitalAnchors,
                        preferredDistrictId: districtId,
                        stableKey: resident.Id.Value);
                    if (primaryCareAnchor is null)
                        continue;

                    requests.Add(
                        new CityPopulationCommuteRouteRequest(
                            ResidentialBuildingId: residentialBuildingId,
                            DestinationAnchorId: primaryCareAnchor.CityAnchorId,
                            Profile: CityPopulationCommuteRoutingProfiles.Pedestrian));
                }
            }

            return requests;
        }

        public static async Task<decimal> EvaluateHousingOpportunityScoreAsync(
            CityId cityId,
            DistrictId districtId,
            ResidentialBuildingId residentialBuildingId,
            DateOnly currentDate,
            IReadOnlyCollection<Person> householdResidents,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            decimal weightedScoreTotal = 0m;
            decimal totalWeight = 0m;

            foreach (Person resident in householdResidents)
            {
                if (!resident.IsAlive)
                    continue;

                if (resident.Employment.Job?.WorkplaceAnchorId is { } workplaceAnchorId)
                {
                    CityPopulationCommuteContext commute = await commuteRoutingService.ResolveAnchorCommuteAsync(
                        cityId: cityId.Value,
                        residentialBuildingId: residentialBuildingId,
                        destinationAnchorId: workplaceAnchorId,
                        cancellationToken: cancellationToken);
                    weightedScoreTotal += ResolveHousingOpportunityContribution(
                        commute: commute,
                        weight: 1.20m);
                    totalWeight += 1.20m;
                }

                if (resident.Education.CurrentInstitutionAnchorId is { } institutionAnchorId)
                {
                    CityPopulationCommuteContext commute = await commuteRoutingService.ResolveAnchorCommuteAsync(
                        cityId: cityId.Value,
                        residentialBuildingId: residentialBuildingId,
                        destinationAnchorId: institutionAnchorId,
                        cancellationToken: cancellationToken);
                    weightedScoreTotal += ResolveHousingOpportunityContribution(
                        commute: commute,
                        weight: 1.00m);
                    totalWeight += 1.00m;
                }

                bool needsHealthcarePriority = resident.HasActiveIllness ||
                                               resident.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior;
                if (!needsHealthcarePriority)
                    continue;

                CityPopulationAnchorCatalogItem? primaryCareAnchor = anchorSelectionPolicy.SelectHospitalAnchor(
                    anchors: hospitalAnchors,
                    preferredDistrictId: districtId,
                    stableKey: resident.Id.Value);
                if (primaryCareAnchor is null)
                    continue;

                CityPopulationCommuteContext healthcareCommute = await commuteRoutingService.ResolveAnchorCommuteAsync(
                    cityId: cityId.Value,
                    residentialBuildingId: residentialBuildingId,
                    destinationAnchorId: primaryCareAnchor.CityAnchorId,
                    cancellationToken: cancellationToken);
                decimal healthcareWeight = resident.CurrentIllnessSeverity == IllnessSeverity.Severe
                    ? 1.10m
                    : resident.HasActiveIllness
                        ? 0.80m
                        : 0.45m;
                weightedScoreTotal += ResolveHousingOpportunityContribution(
                    commute: healthcareCommute,
                    weight: healthcareWeight);
                totalWeight += healthcareWeight;
            }

            if (totalWeight <= 0m)
                return ResolveDistrictHousingStabilityContribution(
                    districtUtilityConditions: ResolveDistrictUtilityConditions(
                        districtId: districtId,
                        districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId));

            CityDistrictUtilityConditionsSnapshot? districtUtilityConditions = ResolveDistrictUtilityConditions(
                districtId: districtId,
                districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId);
            if (districtUtilityConditions is not null)
            {
                weightedScoreTotal += ResolveDistrictHousingStabilityContribution(districtUtilityConditions) * 0.90m;
                totalWeight += 0.90m;
            }

            return decimal.Round(
                d: weightedScoreTotal / totalWeight,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        public static decimal ResolveDistrictHousingStabilityContribution(
            CityDistrictUtilityConditionsSnapshot? districtUtilityConditions)
        {
            if (districtUtilityConditions is null)
                return 0.55m;

            decimal score = (districtUtilityConditions.UtilityIncidentDispatchReadinessIndex * 0.22m) +
                            ((1m - districtUtilityConditions.UtilityIncidentPressureIndex) * 0.26m) +
                            ((1m - districtUtilityConditions.UtilityIncidentCoordinationDifficultyIndex) * 0.14m) +
                            ((1m - districtUtilityConditions.UtilityIncidentRestorationPriorityIndex) * 0.12m) +
                            (districtUtilityConditions.HeatingCoverageIndex * 0.08m) +
                            (districtUtilityConditions.WaterCoverageIndex * 0.08m) +
                            (districtUtilityConditions.PowerCoverageIndex * 0.06m) +
                            (districtUtilityConditions.SanitationCoverageIndex * 0.04m);

            return decimal.Round(
                d: Math.Clamp(
                    value: score,
                    min: 0m,
                    max: 1m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        public static decimal ResolveHousingOpportunityContribution(
            CityPopulationCommuteContext commute,
            decimal weight)
        {
            decimal etaScore = commute.EstimatedTravelTimeMinutes.HasValue
                ? decimal.Clamp(
                    value: 1m - (commute.EstimatedTravelTimeMinutes.Value / 120m),
                    min: 0m,
                    max: 1m)
                : 1m;
            decimal rawScore = (commute.AccessibilityIndex * 0.65m) +
                               (commute.PassabilityIndex * 0.20m) +
                               (etaScore * 0.15m);

            if (!commute.IsAccessible)
                rawScore *= 0.30m;

            return rawScore * weight;
        }

        public static int GetStableInt(
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
    }
}
