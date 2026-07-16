using Matrix.Population.Application.Integration.Education;
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
        internal static async Task<ResidentProgressionStepResult> ApplyAsync(
            PersonEntity person,
            CityId cityId,
            IReadOnlyDictionary<HouseholdId, HouseholdEntity> householdsById,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            EducationParticipationProjectionIndex educationParticipation,
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
            CityEmploymentAutonomyPolicy employmentAutonomyPolicy,
            CityHouseholdPressurePolicy householdPressurePolicy,
            CityPopulationLivingConditionsPressurePolicy livingConditionsPressurePolicy,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> workplaceAnchors,
            IDictionary<string, List<Job>> workplacePools,
            PersonNeedsProgressionPolicy personNeedsProgressionPolicy,
            CityPopulationWeatherExposurePolicy weatherExposurePolicy,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            bool populationChanged = false;
            int externalHealthDelta = 0;
            if (requiresNeedsProgression)
            {
                ResidentProgressionStepResult needsProgression = ResidentNeedsProgressionStep.Apply(
                    person: person,
                    fromSimTimeUtc: fromSimTimeUtc,
                    toSimTimeUtc: toSimTimeUtc,
                    environment: environment,
                    personNeedsProgressionPolicy: personNeedsProgressionPolicy);
                populationChanged |= needsProgression.PopulationChanged;
                externalHealthDelta += needsProgression.ExternalHealthDelta;
            }
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
                    employmentAutonomyPolicy: employmentAutonomyPolicy,
                    workplaceAnchors: workplaceAnchors,
                    workplacePools: workplacePools,
                    commuteRoutingService: commuteRoutingService,
                    cancellationToken: cancellationToken))
                populationChanged = true;
            if (requiresDateProgression &&
                await ResidentHouseholdPressureProgressionStep.ApplyAsync(
                    cityId: cityId,
                    person: person,
                    residentsByHouseholdId: residentsByHouseholdId,
                    educationParticipation: educationParticipation,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                    financialStressByHouseholdId: financialStressByHouseholdId,
                    commuteRoutingService: commuteRoutingService,
                    cancellationToken: cancellationToken,
                    householdPressurePolicy: householdPressurePolicy))
                populationChanged = true;
            if (requiresDateProgression)
            {
                ResidentProgressionStepResult livingConditionsProgression =
                    ResidentLivingConditionsProgressionStep.Apply(
                    person: person,
                    previousDate: previousDate,
                    currentDate: currentDate,
                    housingByHouseholdId: housingByHouseholdId,
                    districtByHouseholdId: districtByHouseholdId,
                    livingConditionsState: livingConditionsState,
                    districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId,
                    essentialsState: essentialsState,
                    districtImpactPolicy: districtImpactPolicy,
                    livingConditionsPressurePolicy: livingConditionsPressurePolicy);
                populationChanged |= livingConditionsProgression.PopulationChanged;
                externalHealthDelta += livingConditionsProgression.ExternalHealthDelta;
            }
            if (exposureSegments.Count > 0)
            {
                ResidentProgressionStepResult weatherExposure = ResidentWeatherExposureStep.Apply(
                    person: person,
                    currentDate: currentDate,
                    environment: environment,
                    exposureSegments: exposureSegments,
                    weatherExposurePolicy: weatherExposurePolicy);
                populationChanged |= weatherExposure.PopulationChanged;
                externalHealthDelta += weatherExposure.ExternalHealthDelta;
            }
            return new ResidentProgressionStepResult(
                PopulationChanged: populationChanged,
                ExternalHealthDelta: Math.Clamp(externalHealthDelta, -100, 100));
        }
    }
}
