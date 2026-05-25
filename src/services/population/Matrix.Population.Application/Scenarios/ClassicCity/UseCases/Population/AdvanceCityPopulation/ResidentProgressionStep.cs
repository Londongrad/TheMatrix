using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentProgressionStep
    {
        internal static async Task<bool> ApplyAsync(
            PersonEntity person,
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            IReadOnlyDictionary<HouseholdId, HouseholdEntity> householdsById,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            DateOnly previousDate,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            DateOnly currentDate,
            bool requiresDateProgression,
            bool requiresNeedsProgression,
            CityPopulationEnvironment? environment,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> employerStressByWorkplaceId,
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> financialStressByHouseholdId,
            CityPopulationCostOfLivingState? costOfLivingState,
            CityPopulationEssentialsState? essentialsState,
            CityPopulationLivingConditionsState? livingConditionsState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>
                districtUtilityConditionsByDistrictId,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            CityPopulationServiceQualityState? serviceQualityState,
            CityPopulationHealthcarePressureProfile healthcarePressureProfile,
            MarriageDomainService marriageDomainService,
            CityEducationAutonomyPolicy educationAutonomyPolicy,
            CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
            CityHouseholdPressurePolicy householdPressurePolicy,
            CityIllnessAutonomyPolicy illnessAutonomyPolicy,
            CityHealthcareAutonomyPolicy healthcareAutonomyPolicy,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            CityPopulationLivingConditionsPressurePolicy livingConditionsPressurePolicy,
            IDictionary<EducationLevel, List<CityEducationInstitutionBinding>> institutionPools,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> workplaceAnchors,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> schoolAnchors,
            IDictionary<string, List<Job>> workplacePools,
            PersonNeedsProgressionPolicy personNeedsProgressionPolicy,
            CityPopulationWeatherExposurePolicy weatherExposurePolicy,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            bool changed = false;
            if (requiresNeedsProgression &&
                ResidentNeedsProgressionStep.Apply(
                    person: person,
                    residentsById: residentsById,
                    fromSimTimeUtc: fromSimTimeUtc,
                    toSimTimeUtc: toSimTimeUtc,
                    currentDate: currentDate,
                    environment: environment,
                    marriageDomainService: marriageDomainService,
                    personNeedsProgressionPolicy: personNeedsProgressionPolicy))
                changed = true;
            if (requiresDateProgression &&
                await ResidentTimeProgressionStep.ApplyAsync(
                    cityId: cityId,
                    person: person,
                    householdsById: householdsById,
                    residentsByHouseholdId: residentsByHouseholdId,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    districtByHouseholdId: districtByHouseholdId,
                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                    employerStressByWorkplaceId: employerStressByWorkplaceId,
                    costOfLivingState: costOfLivingState,
                    serviceQualityState: serviceQualityState,
                    educationAutonomyPolicy: educationAutonomyPolicy,
                    employmentAutonomyPolicy: employmentAutonomyPolicy,
                    institutionPools: institutionPools,
                    workplaceAnchors: workplaceAnchors,
                    schoolAnchors: schoolAnchors,
                    workplacePools: workplacePools,
                    commuteRoutingService: commuteRoutingService,
                    cancellationToken: cancellationToken))
                changed = true;
            if (requiresDateProgression &&
                await ResidentHouseholdPressureProgressionStep.ApplyAsync(
                    cityId: cityId,
                    person: person,
                    residentsByHouseholdId: residentsByHouseholdId,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                    financialStressByHouseholdId: financialStressByHouseholdId,
                    commuteRoutingService: commuteRoutingService,
                    cancellationToken: cancellationToken,
                    householdPressurePolicy: householdPressurePolicy))
                changed = true;
            if (requiresDateProgression &&
                ResidentLivingConditionsProgressionStep.Apply(
                    person: person,
                    residentsById: residentsById,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    districtByHouseholdId: districtByHouseholdId,
                    livingConditionsState: livingConditionsState,
                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                    essentialsState: essentialsState,
                    districtImpactPolicy: districtImpactPolicy,
                    livingConditionsPressurePolicy: livingConditionsPressurePolicy,
                    marriageDomainService: marriageDomainService))
                changed = true;
            if (exposureSegments.Count > 0 &&
                ResidentWeatherExposureStep.Apply(
                    person: person,
                    residentsById: residentsById,
                    currentDate: currentDate,
                    environment: environment,
                    exposureSegments: exposureSegments,
                    marriageDomainService: marriageDomainService,
                    weatherExposurePolicy: weatherExposurePolicy))
                changed = true;
            if (requiresDateProgression &&
                await ResidentIllnessProgressionStep.ApplyAsync(
                    person: person,
                    cityId: cityId,
                    residentsById: residentsById,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    districtByHouseholdId: districtByHouseholdId,
                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                    exposureSegments: exposureSegments,
                    livingConditionsState: livingConditionsState,
                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                    essentialsState: essentialsState,
                    serviceQualityState: serviceQualityState,
                    healthcarePressureProfile: healthcarePressureProfile,
                    marriageDomainService: marriageDomainService,
                    illnessAutonomyPolicy: illnessAutonomyPolicy,
                    healthcareAutonomyPolicy: healthcareAutonomyPolicy,
                    anchorSelectionPolicy: anchorSelectionPolicy,
                    hospitalAnchors: hospitalAnchors,
                    districtImpactPolicy: districtImpactPolicy,
                    livingConditionsPressurePolicy: livingConditionsPressurePolicy,
                    commuteRoutingService: commuteRoutingService,
                    cancellationToken: cancellationToken))
                changed = true;
            return changed;
        }
    }
}
