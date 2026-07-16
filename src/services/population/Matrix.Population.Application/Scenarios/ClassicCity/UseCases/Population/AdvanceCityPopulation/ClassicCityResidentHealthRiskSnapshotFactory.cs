using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Enums;
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
            EducationParticipationProjectionIndex educationParticipation,
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
            CityHealthcareAutonomyPolicy healthcareAutonomyPolicy,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            CityPopulationLivingConditionsPressurePolicy livingConditionsPressurePolicy,
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
                IReadOnlyCollection<PersonEntity> householdResidents =
                    residentsByHouseholdId.TryGetValue(resident.HouseholdId, out var resolvedResidents)
                        ? resolvedResidents
                        : [resident];
                PersonEntity[] aliveHouseholdResidents = householdResidents
                   .Where(member => member.IsAlive)
                   .ToArray();
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
                double healthcareSupportStrength = healthcareAutonomyPolicy.ResolveSupportStrength(
                                                       resident: resident,
                                                       householdResidents: aliveHouseholdResidents,
                                                       housingStatus: housingStatus,
                                                       currentDate: currentDate,
                                                       hasPrimaryCareAccess: prepared.PrimaryCareAnchor is not null,
                                                       hasDistrictPrimaryCareAccess:
                                                       prepared.PrimaryCareAnchor?.DistrictId == prepared.DistrictId,
                                                       districtUtilityConditions: districtUtilityConditions,
                                                       healthcareCommute: healthcareCommute,
                                                       serviceQualityState: serviceQualityState,
                                                       healthcarePressureProfile: healthcarePressureProfile) *
                                                   livingConditionsPressurePolicy.ResolveMedicineAccessStrength(
                                                       livingConditions: districtLivingConditions,
                                                       essentials: districtEssentials);

                snapshots.Add(
                    new PopulationResidentHealthRiskSnapshot(
                        ResidentId: resident.Id.Value,
                        EnergyScore: resident.Energy.Value,
                        HappinessScore: resident.Happiness.Value,
                        StressScore: resident.Stress.Value,
                        SocialNeedScore: resident.SocialNeed.Value,
                        IsVulnerable: resident.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior,
                        HousingStability: MapHousingStability(housingStatus),
                        HasStructuredDailyActivity: resident.Employment.Status == EmploymentStatus.Employed ||
                                                    educationParticipation.FindCurrent(resident)?.IsEnrolled == true,
                        HouseholdSize: Math.Max(1, aliveHouseholdResidents.Length),
                        CaregiverSupportStrength: ResolveCaregiverSupportStrength(
                            resident: resident,
                            householdResidents: aliveHouseholdResidents,
                            currentDate: currentDate),
                        HadAdverseWeatherExposure: hadAdverseWeatherExposure,
                        HealthcareSupportStrength: Math.Clamp(healthcareSupportStrength, 0d, 1d),
                        PublicHealthRiskStrength:
                        livingConditionsPressurePolicy.ResolvePublicHealthRiskStrength(
                            livingConditions: districtLivingConditions,
                            essentials: districtEssentials),
                        LifecycleRevision: resident.LifecycleRevision,
                        CommunityId: prepared.DistrictId?.Value));
            }

            return snapshots;
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
