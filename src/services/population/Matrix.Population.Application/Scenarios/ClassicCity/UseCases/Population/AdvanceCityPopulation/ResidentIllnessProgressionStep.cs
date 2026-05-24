using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;
using PersonId = Matrix.Population.Domain.ValueObjects.PersonId;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentIllnessProgressionStep
    {
        internal static async Task<bool> ApplyAsync(
            PersonEntity person,
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
            CityPopulationLivingConditionsState? livingConditionsState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId,
            CityPopulationEssentialsState? essentialsState,
            CityPopulationServiceQualityState? serviceQualityState,
            CityPopulationHealthcarePressureProfile healthcarePressureProfile,
            MarriageDomainService marriageDomainService,
            CityIllnessAutonomyPolicy illnessAutonomyPolicy,
            CityHealthcareAutonomyPolicy healthcareAutonomyPolicy,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            CityPopulationLivingConditionsPressurePolicy livingConditionsPressurePolicy,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out HousingStatus resolvedHousingStatus)
                ? resolvedHousingStatus
                : null;
            DistrictId? districtId = districtByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out DistrictId? resolvedDistrictId)
                ? resolvedDistrictId
                : null;
            bool hadAdverseExposure = exposureSegments.Any(x => x.Kind == CityWeatherExposureKind.Adverse);
            bool wasAlive = person.IsAlive;
            IReadOnlyCollection<PersonEntity> householdResidents = residentsById.Values
               .Where(x => x.HouseholdId == person.HouseholdId)
               .ToArray();
            CityDistrictUtilityConditionsSnapshot? districtUtilityConditions =
                ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                    districtId: districtId,
                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId);
            CityPopulationLivingConditionsContext districtLivingConditions = districtImpactPolicy.ResolveLivingConditions(
                districtId: districtId,
                livingConditionsState: livingConditionsState,
                districtUtilityConditions: districtUtilityConditions);
            CityPopulationEssentialsContext districtEssentials = districtImpactPolicy.ResolveEssentials(
                districtId: districtId,
                essentialsState: essentialsState);
            CityPopulationAnchorCatalogItem? primaryCareAnchor = anchorSelectionPolicy.SelectHospitalAnchor(
                anchors: hospitalAnchors,
                preferredDistrictId: districtId,
                stableKey: person.Id.Value);
            ResidentialBuildingId? residentialBuildingId = residentialBuildingByHouseholdId.TryGetValue(
                key: person.HouseholdId,
                value: out ResidentialBuildingId? resolvedResidentialBuildingId)
                ? resolvedResidentialBuildingId
                : null;
            CityPopulationCommuteContext healthcareCommute = await commuteRoutingService.ResolveHealthcareCommuteAsync(
                cityId: cityId.Value,
                residentialBuildingId: residentialBuildingId,
                healthcareAnchorId: primaryCareAnchor?.CityAnchorId,
                cancellationToken: cancellationToken);
            double healthcareSupportStrength = healthcareAutonomyPolicy.ResolveSupportStrength(
                resident: person,
                householdResidents: householdResidents,
                housingStatus: housingStatus,
                currentDate: currentDate,
                hasPrimaryCareAccess: primaryCareAnchor is not null,
                hasDistrictPrimaryCareAccess: primaryCareAnchor?.DistrictId == districtId,
                districtUtilityConditions: districtUtilityConditions,
                healthcareCommute: healthcareCommute,
                serviceQualityState: serviceQualityState,
                healthcarePressureProfile: healthcarePressureProfile) *
                  livingConditionsPressurePolicy.ResolveMedicineAccessStrength(
                      livingConditions: districtLivingConditions,
                      essentials: districtEssentials);
            double publicHealthRiskStrength = livingConditionsPressurePolicy.ResolvePublicHealthRiskStrength(
                  livingConditions: districtLivingConditions,
                  essentials: districtEssentials);

            bool changed = illnessAutonomyPolicy.Apply(
                person: person,
                householdResidents: householdResidents,
                previousDate: previousDate,
                currentDate: currentDate,
                housingStatus: housingStatus,
                hadAdverseWeatherExposure: hadAdverseExposure,
                healthcareSupportStrength: healthcareSupportStrength,
                publicHealthRiskStrength: publicHealthRiskStrength);

            if (wasAlive && !person.IsAlive)
                changed = ClassicCityWidowhoodSupport.TryRegisterWidowhood(
                              deceased: person,
                              residentsById: residentsById,
                              marriageDomainService: marriageDomainService) ||
                          changed;

            return changed;
        }
    }
}
