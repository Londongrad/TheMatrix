using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentTimeProgressionStep
    {
        internal static async Task<bool> ApplyAsync(
            CityId cityId,
            PersonEntity person,
            IReadOnlyDictionary<HouseholdId, HouseholdEntity> householdsById,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> employerStressByWorkplaceId,
            CityPopulationCostOfLivingState? costOfLivingState,
            CityPopulationServiceQualityState? serviceQualityState,
            CityEducationAutonomyPolicy educationAutonomyPolicy,
            CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
            IDictionary<EducationLevel, List<CityEducationInstitutionBinding>> institutionPools,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> workplaceAnchors,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> schoolAnchors,
            IDictionary<string, List<Job>> workplacePools,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            bool changed = false;
            if (!person.IsAlive)
                return false;
            IReadOnlyCollection<PersonEntity> householdResidents = residentsByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out IReadOnlyCollection<PersonEntity>? resolvedResidents)
                ? resolvedResidents
                : [person];
            HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out HousingStatus resolvedHousingStatus)
                ? resolvedHousingStatus
                : null;
            ResidentialBuildingId? residentialBuildingId = residentialBuildingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out ResidentialBuildingId? resolvedResidentialBuildingId)
                ? resolvedResidentialBuildingId
                : null;
            if (!householdsById.TryGetValue(
                    key: person.HouseholdId,
                    value: out HouseholdEntity? household))
                return false;
            IReadOnlyList<CityAnchorId> preferredSchoolAnchorIds = await AnchorRouteAccessRanker.RankAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                anchors: schoolAnchors,
                commuteRoutingService: commuteRoutingService,
                cancellationToken: cancellationToken);
            IReadOnlyList<CityAnchorId> preferredWorkplaceAnchorIds = await AnchorRouteAccessRanker.RankAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                anchors: workplaceAnchors,
                commuteRoutingService: commuteRoutingService,
                cancellationToken: cancellationToken);
            if (educationAutonomyPolicy.Apply(
                    person: person,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    institutionPools: institutionPools,
                    preferredDistrictId: districtByHouseholdId.TryGetValue(
                        key: person.HouseholdId,
                        value: out DistrictId? schoolDistrictId)
                        ? schoolDistrictId
                        : null,
                    schoolAnchors: schoolAnchors,
                    preferredInstitutionAnchorIds: preferredSchoolAnchorIds,
                    serviceQualityState: serviceQualityState))
                changed = true;
            if (employmentAutonomyPolicy.Apply(
                    person: person,
                    household: household,
                    householdResidents: householdResidents,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingStatus: housingStatus,
                    preferredDistrictId: districtByHouseholdId.TryGetValue(
                        key: person.HouseholdId,
                        value: out DistrictId? preferredDistrictId)
                        ? preferredDistrictId
                        : null,
                    workplaceAnchors: workplaceAnchors,
                    workplacePools: workplacePools,
                    employerStressByWorkplaceId: employerStressByWorkplaceId,
                    preferredWorkplaceAnchorIds: preferredWorkplaceAnchorIds,
                    costOfLivingState: costOfLivingState))
                changed = true;
            if (person.GetAgeGroup(currentDate) != AgeGroup.Senior)
                return changed;
            if (person.Employment.Status is not (EmploymentStatus.Employed or EmploymentStatus.Student))
                return changed;
            person.Retire(currentDate);
            return true;
        }
    }
}
