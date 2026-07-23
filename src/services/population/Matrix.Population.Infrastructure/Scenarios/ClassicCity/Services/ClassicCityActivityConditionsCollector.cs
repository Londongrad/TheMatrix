using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Services;

internal static class ClassicCityActivityConditionsCollector
{
    internal const int MaxBatchSize = 1000;

    internal static async Task<IReadOnlyList<ClassicCityResidentActivityConditionsBatchV1>> CollectAsync(
        CityId cityId, CityResidentActivityObservation observation, DateTimeOffset occurredAtUtc,
        IReadOnlyCollection<Person> persons, EducationParticipationProjectionIndex participationIndex,
        IReadOnlyCollection<ClassicCityHouseholdPlacement> placements,
        CityPopulationLivingConditionsState? livingState, CityPopulationEssentialsState? essentialsState,
        CityPopulationDistrictImpactPolicy districtPolicy, ICityPopulationCommuteRoutingService routing,
        CancellationToken cancellationToken)
    {
        var students = persons.Where(person => person.IsAlive)
            .Select(person => (Person: person, Participation: participationIndex.FindCurrent(person)))
            .Where(item => item.Participation is { IsEnrolled: true }).ToArray();
        if (students.Length == 0) return [];
        var placementByHousehold = placements.ToDictionary(placement => placement.HouseholdId);
        var routes = new HashSet<CityPopulationCommuteRouteRequest>();
        foreach (var (person, participation) in students)
            if (placementByHousehold.TryGetValue(person.HouseholdId, out var placement)
                && placement.ResidentialBuildingId is { } building && participation!.InstitutionAnchorId is { } anchor)
                routes.Add(new(building, CityAnchorId.From(anchor), CityPopulationCommuteRoutingProfiles.Pedestrian));
        if (routes.Count > 0)
            await routing.PreloadAnchorCommutesAsync(cityId.Value, routes, cancellationToken);

        var areaByDistrict = new Dictionary<Guid, ClassicCityActivityAreaConditionsV1>();
        var batches = new List<ClassicCityResidentActivityConditionsBatchV1>();
        var date = DateOnly.FromDateTime(observation.ObservedAtSimTimeUtc.UtcDateTime);
        int totalBatches = (students.Length + MaxBatchSize - 1) / MaxBatchSize;
        foreach (var chunk in students.Chunk(MaxBatchSize))
        {
            var areas = new List<ClassicCityActivityAreaConditionsV1>();
            var areaIndices = new Dictionary<Guid, int>();
            var facts = new List<ClassicCityResidentActivityConditionsV1>(chunk.Length);
            foreach (var (person, participation) in chunk)
            {
                placementByHousehold.TryGetValue(person.HouseholdId, out var placement);
                var district = placement?.DistrictId;
                Guid areaKey = district?.Value ?? Guid.Empty;
                if (!areaIndices.TryGetValue(areaKey, out int areaIndex))
                {
                    if (!areaByDistrict.TryGetValue(areaKey, out var area))
                    {
                        var living = districtPolicy.ResolveLivingConditions(district, livingState);
                        var essentials = districtPolicy.ResolveEssentials(district, essentialsState);
                        area = new(district?.Value, living.RoadAccessibilityIndex, living.PowerCoverageIndex,
                            living.WaterCoverageIndex, living.HeatingCoverageIndex, living.FloodingIndex,
                            essentials.FoodShortageRiskIndex, essentials.EmergencyWaterShortageRiskIndex,
                            essentials.EmergencyRationingEnabled);
                        areaByDistrict.Add(areaKey, area);
                    }
                    areaIndex = areas.Count;
                    areas.Add(area);
                    areaIndices.Add(areaKey, areaIndex);
                }
                var commute = await routing.ResolveAnchorCommuteAsync(cityId.Value, placement?.ResidentialBuildingId,
                    participation!.InstitutionAnchorId is { } anchor ? CityAnchorId.From(anchor) : null, cancellationToken);
                facts.Add(new(person.Id.Value, person.LifecycleRevision, participation.ParticipationRevision, areaIndex,
                    person.GetAge(date).Years, person.Energy.Value, person.Stress.Value, person.FunctionalCapacity.Value,
                    placement?.HousingStatus == HousingStatus.Homeless, commute.HasRouteData,
                    commute.IsAccessible, commute.AccessibilityIndex));
            }
            batches.Add(new(cityId.Value, observation.SourceTickId, observation.ObservedAtSimTimeUtc, occurredAtUtc,
                batches.Count + 1, totalBatches, areas, facts));
        }
        return batches;
    }
}
