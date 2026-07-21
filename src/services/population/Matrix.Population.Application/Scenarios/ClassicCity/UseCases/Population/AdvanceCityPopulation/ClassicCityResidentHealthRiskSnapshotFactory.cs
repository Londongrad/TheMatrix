using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Enums;
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
    internal static class ClassicCityResidentHealthRiskSnapshotFactory
    {
        internal static async Task<IReadOnlyCollection<PopulationResidentHealthRiskSnapshot>> BuildAsync(
            CityId cityId,
            IReadOnlyCollection<PersonEntity> residents,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            IReadOnlyDictionary<PersonId, PersonRoutineProfile> routineProfilesByResidentId,
            DateOnly currentDate,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            bool hadAdverseWeatherExposure,
            CityPopulationLivingConditionsState? livingConditionsState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>
                districtUtilityConditionsByDistrictId,
            CityPopulationEssentialsState? essentialsState,
            CityPopulationServiceQualityState? serviceQualityState,
            CityPopulationHealthcarePressureProfile healthcarePressureProfile,
            CityHouseholdLivelihoodPolicy householdLivelihoodPolicy,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(residents);
            List<PreparedResident> preparedResidents = residents
               .Where(resident => resident.IsAlive)
               .Select(resident => Prepare(
                    resident: resident,
                    districtByHouseholdId: districtByHouseholdId,
                    residentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                    anchorSelectionPolicy: anchorSelectionPolicy,
                    hospitalAnchors: hospitalAnchors))
               .ToList();
            IReadOnlyDictionary<HouseholdId, PersonEntity[]> aliveResidentsByHouseholdId =
                BuildAliveHouseholdResidents(
                    preparedResidents,
                    residentsByHouseholdId);
            IReadOnlyDictionary<HouseholdId, PopulationResidentHouseholdHealthSnapshot>
                householdHealthByHouseholdId = BuildHouseholdHealthSnapshots(
                    aliveResidentsByHouseholdId,
                    routineProfilesByResidentId,
                    housingByHouseholdId,
                    currentDate,
                    householdLivelihoodPolicy);

            CityPopulationCommuteRouteRequest[] routeRequests = preparedResidents
               .Where(prepared => prepared.ResidentialBuildingId.HasValue
                                  && prepared.PrimaryCareAnchor is not null)
               .Select(prepared => new CityPopulationCommuteRouteRequest(
                    ResidentialBuildingId: prepared.ResidentialBuildingId!.Value,
                    DestinationAnchorId: prepared.PrimaryCareAnchor!.CityAnchorId,
                    Profile: CityPopulationCommuteRoutingProfiles.Pedestrian))
               .Distinct()
               .ToArray();
            await commuteRoutingService.PreloadAnchorCommutesAsync(
                cityId: cityId.Value,
                requests: routeRequests,
                cancellationToken: cancellationToken);

            List<PopulationResidentHealthRiskSnapshot> snapshots = new(preparedResidents.Count);
            foreach (PreparedResident prepared in preparedResidents)
            {
                PersonEntity resident = prepared.Resident;
                PersonEntity[] aliveHouseholdResidents = aliveResidentsByHouseholdId[resident.HouseholdId];
                HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                    resident.HouseholdId,
                    out HousingStatus resolvedHousingStatus)
                    ? resolvedHousingStatus
                    : null;
                CityDistrictUtilityConditionsSnapshot? districtUtilityConditions =
                    ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                        districtId: prepared.DistrictId,
                        districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId);
                CityPopulationLivingConditionsContext districtLivingConditions =
                    districtImpactPolicy.ResolveLivingConditions(
                        districtId: prepared.DistrictId,
                        livingConditionsState: livingConditionsState,
                        districtUtilityConditions: districtUtilityConditions);
                CityPopulationEssentialsContext districtEssentials = districtImpactPolicy.ResolveEssentials(
                    districtId: prepared.DistrictId,
                    essentialsState: essentialsState);
                CityPopulationCommuteContext healthcareCommute =
                    await commuteRoutingService.ResolveHealthcareCommuteAsync(
                        cityId: cityId.Value,
                        residentialBuildingId: prepared.ResidentialBuildingId,
                        healthcareAnchorId: prepared.PrimaryCareAnchor?.CityAnchorId,
                        cancellationToken: cancellationToken);
                bool hasStructuredDailyActivity =
                    routineProfilesByResidentId.TryGetValue(
                        key: resident.Id,
                        value: out PersonRoutineProfile? routineProfile) &&
                    routineProfile.HasStructuredActivity;

                snapshots.Add(
                    new PopulationResidentHealthRiskSnapshot(
                        ResidentId: resident.Id.Value,
                        EnergyScore: resident.Energy.Value,
                        HappinessScore: resident.Happiness.Value,
                        StressScore: resident.Stress.Value,
                        SocialNeedScore: resident.SocialNeed.Value,
                        IsVulnerable: resident.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior,
                        HousingStability: MapHousingStability(housingStatus),
                        HasStructuredDailyActivity: hasStructuredDailyActivity,
                        HouseholdSize: Math.Max(1, aliveHouseholdResidents.Length),
                        CaregiverSupportStrength: ResolveCaregiverSupportStrength(
                            resident: resident,
                            householdResidents: aliveHouseholdResidents,
                            currentDate: currentDate),
                        HadAdverseWeatherExposure: hadAdverseWeatherExposure,
                        LifecycleRevision: resident.LifecycleRevision,
                        CommunityId: prepared.DistrictId?.Value,
                        FunctionalCapacityScore: resident.FunctionalCapacity.Value,
                        IsEmployed: resident.Employment.Status == EmploymentStatus.Employed,
                        Household: householdHealthByHouseholdId[resident.HouseholdId],
                        HealthcareAccess: MapHealthcareAccess(
                            prepared,
                            healthcareCommute,
                            districtUtilityConditions,
                            serviceQualityState,
                            healthcarePressureProfile),
                        Environment: MapEnvironment(
                            districtLivingConditions,
                            districtEssentials)));
            }

            return snapshots;
        }

        private static IReadOnlyDictionary<HouseholdId, PersonEntity[]> BuildAliveHouseholdResidents(
            IReadOnlyCollection<PreparedResident> preparedResidents,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId)
        {
            return preparedResidents
               .GroupBy(prepared => prepared.Resident.HouseholdId)
               .ToDictionary(
                    group => group.Key,
                    group => residentsByHouseholdId.TryGetValue(group.Key, out var householdResidents)
                        ? householdResidents.Where(member => member.IsAlive).ToArray()
                        : group.Select(prepared => prepared.Resident).ToArray());
        }

        private static IReadOnlyDictionary<HouseholdId, PopulationResidentHouseholdHealthSnapshot>
            BuildHouseholdHealthSnapshots(
                IReadOnlyDictionary<HouseholdId, PersonEntity[]> aliveResidentsByHouseholdId,
                IReadOnlyDictionary<PersonId, PersonRoutineProfile> routineProfilesByResidentId,
                IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
                DateOnly currentDate,
                CityHouseholdLivelihoodPolicy householdLivelihoodPolicy)
        {
            return aliveResidentsByHouseholdId.ToDictionary(
                pair => pair.Key,
                pair =>
                {
                    HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                        pair.Key,
                        out HousingStatus resolvedHousingStatus)
                        ? resolvedHousingStatus
                        : null;
                    CityHouseholdLivelihoodProfile livelihood = householdLivelihoodPolicy.Build(
                        householdResidents: pair.Value,
                        routineProfilesByResidentId: routineProfilesByResidentId,
                        housingStatus: housingStatus,
                        currentDate: currentDate);

                    return new PopulationResidentHouseholdHealthSnapshot(
                        StabilityScore: livelihood.StabilityScore,
                        AdultProviderCount: livelihood.AdultProviderCount,
                        AdultStructuredParticipantCount: livelihood.AdultStructuredParticipantCount,
                        FunctionalLimitationCount: livelihood.FunctionalLimitationCount,
                        HasStructuredSupport: livelihood.HasStructuredSupport);
                });
        }

        private static PopulationResidentHealthcareAccessSnapshot MapHealthcareAccess(
            PreparedResident prepared,
            CityPopulationCommuteContext commute,
            CityDistrictUtilityConditionsSnapshot? utilityConditions,
            CityPopulationServiceQualityState? serviceQualityState,
            CityPopulationHealthcarePressureProfile healthcarePressureProfile)
        {
            return new PopulationResidentHealthcareAccessSnapshot(
                HasPrimaryCareDestination: prepared.PrimaryCareAnchor is not null,
                IsPrimaryCareInCommunity:
                prepared.PrimaryCareAnchor?.DistrictId == prepared.DistrictId,
                HasRouteData: commute.HasRouteData,
                IsRouteAccessible: commute.IsAccessible,
                RouteAccessibilityIndex: (double)commute.AccessibilityIndex,
                RoutePassabilityIndex: (double)commute.PassabilityIndex,
                EstimatedTravelTimeMinutes: (double?)commute.EstimatedTravelTimeMinutes,
                HasInfrastructureData: utilityConditions is not null,
                UtilityIncidentDispatchReadinessIndex:
                (double)(utilityConditions?.UtilityIncidentDispatchReadinessIndex ?? 1m),
                UtilityIncidentPressureIndex:
                (double)(utilityConditions?.UtilityIncidentPressureIndex ?? 0m),
                UtilityIncidentCoordinationDifficultyIndex:
                (double)(utilityConditions?.UtilityIncidentCoordinationDifficultyIndex ?? 0m),
                UtilityIncidentRestorationPriorityIndex:
                (double)(utilityConditions?.UtilityIncidentRestorationPriorityIndex ?? 0m),
                PowerCoverageIndex: (double)(utilityConditions?.PowerCoverageIndex ?? 1m),
                WaterCoverageIndex: (double)(utilityConditions?.WaterCoverageIndex ?? 1m),
                HeatingCoverageIndex: (double)(utilityConditions?.HeatingCoverageIndex ?? 1m),
                SanitationCoverageIndex: (double)(utilityConditions?.SanitationCoverageIndex ?? 1m),
                HealthcareQualityIndex: (double)(serviceQualityState?.HealthcareQualityIndex ?? 1m),
                RecoverySupportIndex: (double)healthcarePressureProfile.RecoverySupportIndex,
                TriagePressureIndex: (double)healthcarePressureProfile.TriagePressureIndex);
        }

        private static PopulationResidentEnvironmentalHealthSnapshot MapEnvironment(
            CityPopulationLivingConditionsContext livingConditions,
            CityPopulationEssentialsContext essentials)
        {
            return new PopulationResidentEnvironmentalHealthSnapshot(
                WaterCoverageIndex: (double)livingConditions.WaterCoverageIndex,
                SanitationCoverageIndex: (double)livingConditions.SanitationCoverageIndex,
                FloodingIndex: (double)livingConditions.FloodingIndex,
                UtilityContinuityIndex: (double)livingConditions.UtilityContinuityIndex,
                EmergencyWaterShortageRiskIndex:
                (double)essentials.EmergencyWaterShortageRiskIndex,
                FoodShortageRiskIndex: (double)essentials.FoodShortageRiskIndex,
                MedicineShortageRiskIndex: (double)essentials.MedicineShortageRiskIndex,
                EmergencyRationingEnabled: essentials.EmergencyRationingEnabled);
        }

        private static PreparedResident Prepare(
            PersonEntity resident,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors)
        {
            DistrictId? districtId = districtByHouseholdId.TryGetValue(
                resident.HouseholdId,
                out DistrictId? resolvedDistrictId)
                ? resolvedDistrictId
                : null;
            ResidentialBuildingId? residentialBuildingId = residentialBuildingByHouseholdId.TryGetValue(
                resident.HouseholdId,
                out ResidentialBuildingId? resolvedResidentialBuildingId)
                ? resolvedResidentialBuildingId
                : null;

            return new PreparedResident(
                Resident: resident,
                DistrictId: districtId,
                ResidentialBuildingId: residentialBuildingId,
                PrimaryCareAnchor: anchorSelectionPolicy.SelectHospitalAnchor(
                    anchors: hospitalAnchors,
                    preferredDistrictId: districtId,
                    stableKey: resident.Id.Value));
        }

        private static string MapHousingStability(HousingStatus? housingStatus) =>
            housingStatus switch
            {
                HousingStatus.Housed => "Housed",
                HousingStatus.Homeless => "Unhoused",
                _ => "Unknown"
            };

        private static double ResolveCaregiverSupportStrength(
            PersonEntity resident,
            IReadOnlyCollection<PersonEntity> householdResidents,
            DateOnly currentDate)
        {
            PersonEntity[] caregivers = householdResidents
               .Where(member => member.Id != resident.Id
                                && member.GetAgeGroup(currentDate) is AgeGroup.Adult or AgeGroup.Senior
                                && member.FunctionalCapacity.Value >= 50)
               .ToArray();
            if (caregivers.Length == 0)
                return 0d;

            double strength = Math.Min(0.18d, caregivers.Length * 0.06d);
            if (resident.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior)
                strength += 0.04d;
            if (caregivers.Any(member =>
                    member.Id == resident.SpouseId
                    || member.Id == resident.MotherId
                    || member.Id == resident.FatherId
                    || resident.MotherId == member.Id
                    || resident.FatherId == member.Id))
                strength += 0.03d;

            return Math.Clamp(strength, 0d, 0.28d);
        }

        private sealed record PreparedResident(
            PersonEntity Resident,
            DistrictId? DistrictId,
            ResidentialBuildingId? ResidentialBuildingId,
            CityPopulationAnchorCatalogItem? PrimaryCareAnchor);
    }
}
