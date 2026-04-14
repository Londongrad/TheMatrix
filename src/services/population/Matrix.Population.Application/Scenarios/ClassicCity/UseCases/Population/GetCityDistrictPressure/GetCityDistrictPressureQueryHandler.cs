using System.Globalization;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDistrictPressure
{
    public sealed class GetCityDistrictPressureQueryHandler(
        ICityPopulationPersonReadRepository personReadRepository,
        IHouseholdWriteRepository householdWriteRepository,
        ICityDistrictUtilityConditionsClient districtUtilityConditionsClient,
        ILogger<GetCityDistrictPressureQueryHandler> logger)
        : IRequestHandler<GetCityDistrictPressureQuery, CityPopulationDistrictPressureDto?>
    {
        public async Task<CityPopulationDistrictPressureDto?> Handle(
            GetCityDistrictPressureQuery request,
            CancellationToken cancellationToken)
        {
            var cityId = CityId.From(request.CityId);

            IReadOnlyCollection<Person> persons = await personReadRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            IReadOnlyCollection<ClassicCityHouseholdPlacement> placements =
                await householdWriteRepository.ListPlacementsByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            if (persons.Count == 0 || placements.Count == 0)
                return null;

            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId =
                new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>();

            try
            {
                districtUtilityConditionsByDistrictId =
                    await districtUtilityConditionsClient.GetByCityAsync(
                        cityId: request.CityId,
                        cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to load district utility conditions for district pressure query, cityId={CityId}. Falling back to resident-only pressure.",
                    request.CityId);
            }

            IReadOnlyDictionary<HouseholdId, ClassicCityHouseholdPlacement> placementByHouseholdId = placements
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.Last());

            Person[] aliveResidents = persons
               .Where(x => x.IsAlive)
               .ToArray();

            CityPopulationDistrictPressureItemDto[] districts = aliveResidents
               .Where(x =>
                    placementByHouseholdId.TryGetValue(
                        key: x.HouseholdId,
                        value: out ClassicCityHouseholdPlacement? placement) &&
                    placement.DistrictId.HasValue)
               .GroupBy(x => placementByHouseholdId[x.HouseholdId].DistrictId!.Value)
               .Select(x => CreateDistrictPressureItem(
                    districtId: x.Key,
                    residents: x.ToArray(),
                    placementByHouseholdId: placementByHouseholdId,
                    districtUtilityConditions: ResolveDistrictUtilityConditions(
                        districtId: x.Key,
                        districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId)))
               .OrderByDescending(x => x.PopulationPressureIndex)
               .ThenByDescending(x => x.HomelessResidentCount)
               .ThenByDescending(x => x.ActiveIllnessCount)
               .ThenBy(x => x.DistrictId)
               .ToArray();

            return new CityPopulationDistrictPressureDto(
                CityId: request.CityId,
                GeneratedAtUtc: FormatTimestamp(DateTimeOffset.UtcNow)!,
                Districts: districts);
        }

        private static CityPopulationDistrictPressureItemDto CreateDistrictPressureItem(
            DistrictId districtId,
            IReadOnlyCollection<Person> residents,
            IReadOnlyDictionary<HouseholdId, ClassicCityHouseholdPlacement> placementByHouseholdId,
            CityDistrictUtilityConditionsSnapshot? districtUtilityConditions)
        {
            int residentCount = residents.Count;
            int householdCount = residents
               .Select(x => x.HouseholdId)
               .Distinct()
               .Count();
            int homelessResidentCount = residents.Count(x =>
                placementByHouseholdId.TryGetValue(
                    key: x.HouseholdId,
                    value: out ClassicCityHouseholdPlacement? placement) &&
                placement.HousingStatus == HousingStatus.Homeless);
            int activeIllnessCount = residents.Count(x => x.HasActiveIllness);
            int severeIllnessCount = residents.Count(x => x.CurrentIllnessSeverity == IllnessSeverity.Severe);
            decimal averageHealth = RoundMetric(residents.Average(x => (decimal)x.Health.Value));
            decimal averageStress = RoundMetric(residents.Average(x => (decimal)x.Stress.Value));
            decimal averageHappiness = RoundMetric(residents.Average(x => (decimal)x.Happiness.Value));
            decimal utilityContinuityIndex = RoundMetric(ResolveUtilityContinuityIndex(districtUtilityConditions));
            decimal utilityIncidentPressureIndex = RoundMetric(
                districtUtilityConditions?.UtilityIncidentPressureIndex ?? 0m);
            decimal housingFragilityIndex = RoundMetric(ResolveHousingFragilityIndex(districtUtilityConditions));
            decimal populationPressureIndex = RoundMetric(
                ResolvePopulationPressureIndex(
                    residentCount: residentCount,
                    homelessResidentCount: homelessResidentCount,
                    activeIllnessCount: activeIllnessCount,
                    severeIllnessCount: severeIllnessCount,
                    averageHealth: averageHealth,
                    averageStress: averageStress,
                    utilityContinuityIndex: utilityContinuityIndex,
                    utilityIncidentPressureIndex: utilityIncidentPressureIndex,
                    housingFragilityIndex: housingFragilityIndex));

            return new CityPopulationDistrictPressureItemDto(
                DistrictId: districtId.Value,
                ResidentCount: residentCount,
                HouseholdCount: householdCount,
                HomelessResidentCount: homelessResidentCount,
                AverageHealth: averageHealth,
                AverageStress: averageStress,
                AverageHappiness: averageHappiness,
                ActiveIllnessCount: activeIllnessCount,
                SevereIllnessCount: severeIllnessCount,
                UtilityContinuityIndex: utilityContinuityIndex,
                UtilityIncidentPressureIndex: utilityIncidentPressureIndex,
                HousingFragilityIndex: housingFragilityIndex,
                PopulationPressureIndex: populationPressureIndex);
        }

        private static decimal ResolveUtilityContinuityIndex(
            CityDistrictUtilityConditionsSnapshot? districtUtilityConditions)
        {
            if (districtUtilityConditions is null)
                return 1m;

            decimal continuity = (districtUtilityConditions.PowerCoverageIndex * 0.34m) +
                                 (districtUtilityConditions.HeatingCoverageIndex * 0.18m) +
                                 (districtUtilityConditions.WaterCoverageIndex * 0.20m) +
                                 (districtUtilityConditions.SanitationCoverageIndex * 0.16m) +
                                 ((1m - districtUtilityConditions.PowerOutageRiskIndex) * 0.05m) +
                                 ((1m - districtUtilityConditions.WaterDisruptionRiskIndex) * 0.04m) +
                                 ((1m - districtUtilityConditions.SanitationContaminationRiskIndex) * 0.03m);
            decimal incidentAdjustment = (districtUtilityConditions.UtilityIncidentDispatchReadinessIndex * 0.12m) -
                                         (districtUtilityConditions.UtilityIncidentPressureIndex * 0.16m) -
                                         (districtUtilityConditions.UtilityIncidentCoordinationDifficultyIndex * 0.08m) -
                                         (districtUtilityConditions.UtilityIncidentRestorationPriorityIndex * 0.06m);

            return decimal.Clamp(
                value: continuity + incidentAdjustment,
                min: 0m,
                max: 1.5m);
        }

        private static decimal ResolveHousingFragilityIndex(
            CityDistrictUtilityConditionsSnapshot? districtUtilityConditions)
        {
            if (districtUtilityConditions is null)
                return 0m;

            decimal fragility = ((1m - districtUtilityConditions.UtilityIncidentDispatchReadinessIndex) * 0.26m) +
                                (districtUtilityConditions.UtilityIncidentPressureIndex * 0.34m) +
                                (districtUtilityConditions.UtilityIncidentCoordinationDifficultyIndex * 0.22m) +
                                (districtUtilityConditions.UtilityIncidentRestorationPriorityIndex * 0.18m);

            return decimal.Clamp(
                value: fragility,
                min: 0m,
                max: 1m);
        }

        private static decimal ResolvePopulationPressureIndex(
            int residentCount,
            int homelessResidentCount,
            int activeIllnessCount,
            int severeIllnessCount,
            decimal averageHealth,
            decimal averageStress,
            decimal utilityContinuityIndex,
            decimal utilityIncidentPressureIndex,
            decimal housingFragilityIndex)
        {
            if (residentCount <= 0)
                return 0m;

            decimal lowHealthIndex = 1m - decimal.Clamp(averageHealth / 100m, 0m, 1m);
            decimal stressIndex = decimal.Clamp(averageStress / 100m, 0m, 1m);
            decimal illnessBurdenIndex = decimal.Clamp((decimal)activeIllnessCount / residentCount, 0m, 1m);
            decimal severeIllnessBurdenIndex = decimal.Clamp((decimal)severeIllnessCount / residentCount, 0m, 1m);
            decimal homelessnessIndex = decimal.Clamp((decimal)homelessResidentCount / residentCount, 0m, 1m);
            decimal utilityFragilityIndex = decimal.Clamp(1m - decimal.Clamp(utilityContinuityIndex, 0m, 1m), 0m, 1m);

            return decimal.Clamp(
                value: (stressIndex * 0.24m) +
                       (lowHealthIndex * 0.16m) +
                       (illnessBurdenIndex * 0.16m) +
                       (severeIllnessBurdenIndex * 0.12m) +
                       (homelessnessIndex * 0.10m) +
                       (utilityFragilityIndex * 0.12m) +
                       (utilityIncidentPressureIndex * 0.10m) +
                       (housingFragilityIndex * 0.10m),
                min: 0m,
                max: 1m);
        }

        private static CityDistrictUtilityConditionsSnapshot? ResolveDistrictUtilityConditions(
            DistrictId districtId,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId)
        {
            return districtUtilityConditionsByDistrictId.TryGetValue(
                key: districtId,
                value: out CityDistrictUtilityConditionsSnapshot? snapshot)
                ? snapshot
                : null;
        }

        private static decimal RoundMetric(decimal value)
        {
            return decimal.Round(
                d: value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static string? FormatTimestamp(DateTimeOffset? value)
        {
            return value?.ToString(
                format: "O",
                formatProvider: CultureInfo.InvariantCulture);
        }
    }
}
