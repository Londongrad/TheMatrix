using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class HousingAutonomyStep
    {
        internal static async Task<int> ApplyAsync(
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            IReadOnlyDictionary<PersonId, PersonRoutineProfile> routineProfilesByResidentId,
            EducationParticipationProjectionIndex educationParticipation,
            DateOnly previousDate,
            DateOnly currentDate,
            IHouseholdWriteRepository householdWriteRepository,
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> financialStressByHouseholdId,
            CityPopulationCostOfLivingState? costOfLivingState,
            CityPopulationServiceQualityState? serviceQualityState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>
                districtUtilityConditionsByDistrictId,
            CityHousingAutonomyPolicy housingAutonomyPolicy,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            ICollection<CityPopulationActivityWriteModel> activityEntries,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ClassicCityHouseholdPlacement> placements =
                await householdWriteRepository.ListPlacementsByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            if (placements.Count == 0)
                return 0;

            var housingStatuses = placements.ToDictionary(
                keySelector: x => x.HouseholdId,
                elementSelector: x => x.HousingStatus);
            var householdsById = (await householdWriteRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken)).ToDictionary(
                keySelector: x => x.Id,
                elementSelector: x => x);
            var residentsByHousehold = residentsById.Values
               .Where(x => x.IsAlive)
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.ToList());
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId =
                placements.ToDictionary(
                    keySelector: x => x.HouseholdId,
                    elementSelector: x => x.ResidentialBuildingId);
            IReadOnlyDictionary<HouseholdId, CityDistrictUtilityConditionsSnapshot>
                districtUtilityConditionsByHouseholdId =
                    placements
                       .Where(x => x.DistrictId.HasValue)
                       .Select(x => new
                       {
                           x.HouseholdId,
                           Snapshot = ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                                districtId: x.DistrictId,
                                districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId)
                       })
                       .Where(x => x.Snapshot is not null)
                       .ToDictionary(
                            keySelector: x => x.HouseholdId,
                            elementSelector: x => x.Snapshot!);
            var commutePressureProfilesByHouseholdId =
                new Dictionary<HouseholdId, CityHouseholdCommutePressureProfile>();

            foreach (ClassicCityHouseholdPlacement placement in placements)
            {
                if (!residentsByHousehold.TryGetValue(
                        key: placement.HouseholdId,
                        value: out List<PersonEntity>? householdResidents) ||
                    householdResidents.Count == 0)
                    continue;

                CityHouseholdCommutePressureProfile? commutePressureProfile =
                    await HouseholdCommutePressureProfileBuilder.BuildAsync(
                        cityId: cityId,
                        householdId: placement.HouseholdId,
                        householdResidents: householdResidents,
                        educationParticipation: educationParticipation,
                        residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                        commuteRoutingService: commuteRoutingService,
                        cancellationToken: cancellationToken);

                if (commutePressureProfile is not null)
                    commutePressureProfilesByHouseholdId[placement.HouseholdId] = commutePressureProfile;
            }

            IReadOnlyList<CityHousingAutonomyDecision> decisions = housingAutonomyPolicy.Plan(
                households: householdsById,
                residents: residentsById.Values.ToArray(),
                routineProfilesByResidentId: routineProfilesByResidentId,
                housingStatuses: housingStatuses,
                financialStressStates: financialStressByHouseholdId,
                commutePressureProfiles: commutePressureProfilesByHouseholdId,
                districtUtilityConditionsByHouseholdId: districtUtilityConditionsByHouseholdId,
                previousDate: previousDate,
                currentDate: currentDate,
                costOfLivingState: costOfLivingState,
                serviceQualityState: serviceQualityState);

            if (decisions.Count == 0)
                return 0;

            var placementsByHousehold = placements.ToDictionary(
                keySelector: x => x.HouseholdId,
                elementSelector: x => x);
            IReadOnlyList<CityPopulationAnchorCatalogItem> hospitalAnchors =
                await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                    cityId: cityId,
                    type: CityAnchorType.Hospital,
                    cancellationToken: cancellationToken);

            List<(DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> housingPool =
                ClassicCityHousingOpportunityPlanner.BuildHousingOpportunityPool(placements);

            int affectedResidents = 0;

            foreach (CityHousingAutonomyDecision decision in decisions)
            {
                if (!placementsByHousehold.TryGetValue(
                        key: decision.HouseholdId,
                        value: out ClassicCityHouseholdPlacement? placement) ||
                    !residentsByHousehold.TryGetValue(
                        key: decision.HouseholdId,
                        value: out List<PersonEntity>? householdResidents) ||
                    householdResidents.Count == 0)
                    continue;

                PersonEntity anchorResident = ClassicCityHousingOpportunityPlanner.SelectHousingAnchorResident(
                    householdResidents: householdResidents,
                    currentDate: currentDate);

                switch (decision.Type)
                {
                    case CityHousingAutonomyDecisionType.FindHousing:
                        if (placement.HousingStatus == HousingStatus.Housed ||
                            housingPool.Count == 0)
                            continue;

                        (DistrictId districtId, ResidentialBuildingId residentialBuildingId) opportunity =
                            await ClassicCityHousingOpportunityPlanner.SelectHousingOpportunityAsync(
                                cityId: cityId,
                                householdId: placement.HouseholdId,
                                currentDate: currentDate,
                                housingPool: housingPool,
                                householdResidents: householdResidents,
                                educationParticipation: educationParticipation,
                                hospitalAnchors: hospitalAnchors,
                                districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                                anchorSelectionPolicy: anchorSelectionPolicy,
                                commuteRoutingService: commuteRoutingService,
                                cancellationToken: cancellationToken);

                        placement.Relocate(
                            cityId: cityId,
                            districtId: opportunity.districtId,
                            residentialBuildingId: opportunity.residentialBuildingId);
                        activityEntries.Add(
                            ClassicCityActivityFactory.HouseholdFoundHousing(
                                cityId: cityId.Value,
                                currentDate: currentDate,
                                resident: anchorResident,
                                source: CityPopulationActivitySource.Autonomy,
                                occurredAtUtc: occurredAtUtc));
                        affectedResidents += householdResidents.Count;
                        break;

                    case CityHousingAutonomyDecisionType.LoseHousing:
                        if (placement.HousingStatus != HousingStatus.Housed)
                            continue;

                        placement.BecomeHomeless(cityId);
                        activityEntries.Add(
                            ClassicCityActivityFactory.HouseholdLostHousing(
                                cityId: cityId.Value,
                                currentDate: currentDate,
                                resident: anchorResident,
                                source: CityPopulationActivitySource.Autonomy,
                                occurredAtUtc: occurredAtUtc));
                        affectedResidents += householdResidents.Count;
                        break;
                }
            }

            return affectedResidents;
        }
    }
}
