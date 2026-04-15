using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services.World
{
    public sealed class CityPopulationCommuteTripSyncService(
        ICityPopulationActiveTripClient activeTripClient,
        ICityPopulationCommuteRoutingService commuteRoutingService)
        : ICityPopulationCommuteTripSyncService
    {
        private const int MaxTripsPerTick = 12;
        private const decimal MinAccessibilityIndex = 0.28m;
        private const string PedestrianProfile = "Pedestrian";
        private const string ResidentialBuildingPointKind = "ResidentialBuilding";
        private const string CityAnchorPointKind = "CityAnchor";
        private const string HealthcareAccessPurpose = "HealthcareAccess";
        private const string WorkCommutePurpose = "WorkCommute";
        private const string EducationCommutePurpose = "EducationCommute";

        public async Task SyncAsync(
            Guid cityId,
            long tickId,
            DateOnly currentDate,
            DateTimeOffset currentSimTimeUtc,
            IReadOnlyCollection<Person> residents,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            CancellationToken cancellationToken)
        {
            if (residents.Count == 0 || householdPlacements.Count == 0)
                return;

            MobilityPhaseWindow phaseWindow = ResolvePhaseWindow(currentSimTimeUtc);
            if (!phaseWindow.HasAnyDispatch)
                return;

            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId =
                householdPlacements
                   .GroupBy(x => x.HouseholdId)
                   .ToDictionary(
                        keySelector: x => x.Key,
                        elementSelector: x => x.Select(y => y.ResidentialBuildingId)
                           .FirstOrDefault());
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId = householdPlacements
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.Select(y => y.DistrictId)
                       .FirstOrDefault());

            IReadOnlyCollection<CityPopulationActiveTripSnapshot> activeTrips =
                await activeTripClient.ListActiveByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            var activeTripKeys = activeTrips
               .Where(x => x.TravellerEntityId.HasValue)
               .Select(x => BuildTripConcurrencyKey(
                    travellerEntityId: x.TravellerEntityId!.Value,
                    purpose: x.Purpose))
               .ToHashSet(StringComparer.Ordinal);

            List<CommuteTripCandidate> candidates = [];

            foreach (Person resident in residents)
            {
                if (!resident.IsAlive)
                    continue;

                if (!residentialBuildingByHouseholdId.TryGetValue(
                        key: resident.HouseholdId,
                        value: out ResidentialBuildingId? residentialBuildingId) ||
                    !residentialBuildingId.HasValue)
                    continue;

                string healthcareTripKey = BuildTripConcurrencyKey(
                    travellerEntityId: resident.Id.Value,
                    purpose: HealthcareAccessPurpose);
                if (phaseWindow.ShouldDispatchHealthcare &&
                    !activeTripKeys.Contains(healthcareTripKey))
                    await TryAddHealthcareCandidateAsync(
                        cityId: cityId,
                        tickId: tickId,
                        currentDate: currentDate,
                        resident: resident,
                        residentialBuildingId: residentialBuildingId.Value,
                        preferredDistrictId: districtByHouseholdId.TryGetValue(
                            key: resident.HouseholdId,
                            value: out DistrictId? preferredDistrictId)
                            ? preferredDistrictId
                            : null,
                        hospitalAnchors: hospitalAnchors,
                        anchorSelectionPolicy: anchorSelectionPolicy,
                        candidates: candidates,
                        cancellationToken: cancellationToken);

                string workTripKey = BuildTripConcurrencyKey(
                    travellerEntityId: resident.Id.Value,
                    purpose: WorkCommutePurpose);
                if (phaseWindow.ShouldDispatchOutboundCommutes &&
                    !activeTripKeys.Contains(workTripKey))
                    await TryAddEmploymentCandidateAsync(
                        cityId: cityId,
                        tickId: tickId,
                        resident: resident,
                        residentialBuildingId: residentialBuildingId.Value,
                        candidates: candidates,
                        cancellationToken: cancellationToken);
                else if (phaseWindow.ShouldDispatchReturnCommutes &&
                         !activeTripKeys.Contains(workTripKey))
                    TryAddEmploymentReturnCandidate(
                        tickId: tickId,
                        resident: resident,
                        residentialBuildingId: residentialBuildingId.Value,
                        candidates: candidates);

                string educationTripKey = BuildTripConcurrencyKey(
                    travellerEntityId: resident.Id.Value,
                    purpose: EducationCommutePurpose);
                if (phaseWindow.ShouldDispatchOutboundCommutes &&
                    !activeTripKeys.Contains(educationTripKey))
                    await TryAddEducationCandidateAsync(
                        cityId: cityId,
                        tickId: tickId,
                        resident: resident,
                        residentialBuildingId: residentialBuildingId.Value,
                        candidates: candidates,
                        cancellationToken: cancellationToken);
                else if (phaseWindow.ShouldDispatchReturnCommutes &&
                         !activeTripKeys.Contains(educationTripKey))
                    TryAddEducationReturnCandidate(
                        tickId: tickId,
                        resident: resident,
                        residentialBuildingId: residentialBuildingId.Value,
                        candidates: candidates);
            }

            foreach (CommuteTripCandidate candidate in candidates
                        .OrderBy(x => x.Priority)
                        .ThenBy(x => x.OrderingKey)
                        .Take(MaxTripsPerTick))
            {
                string concurrencyKey = BuildTripConcurrencyKey(
                    travellerEntityId: candidate.TravellerEntityId,
                    purpose: candidate.Purpose);
                if (activeTripKeys.Contains(concurrencyKey))
                    continue;

                bool dispatched = await activeTripClient.TryDispatchAsync(
                    request: new CityPopulationTripDispatchRequest(
                        CityId: cityId,
                        FromKind: candidate.FromKind,
                        FromId: candidate.FromId,
                        ToKind: candidate.ToKind,
                        ToId: candidate.ToId,
                        Purpose: candidate.Purpose,
                        Profile: PedestrianProfile,
                        MovementCapabilityIndex: candidate.MovementCapabilityIndex,
                        TravellerEntityId: candidate.TravellerEntityId,
                        Subject: candidate.Subject),
                    cancellationToken: cancellationToken);

                if (dispatched)
                    activeTripKeys.Add(concurrencyKey);
            }
        }

        private async Task TryAddEmploymentCandidateAsync(
            Guid cityId,
            long tickId,
            Person resident,
            ResidentialBuildingId residentialBuildingId,
            ICollection<CommuteTripCandidate> candidates,
            CancellationToken cancellationToken)
        {
            if (resident.Employment.Status != EmploymentStatus.Employed ||
                resident.Employment.Job?.WorkplaceAnchorId is not { } workplaceAnchorId)
                return;

            CityPopulationCommuteContext commute = await commuteRoutingService.ResolveEmploymentCommuteAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                resident: resident,
                cancellationToken: cancellationToken);
            if (!ShouldMaterializeCommuteTrip(commute))
                return;

            candidates.Add(
                new CommuteTripCandidate(
                    TravellerEntityId: resident.Id.Value,
                    FromKind: ResidentialBuildingPointKind,
                    FromId: residentialBuildingId.Value,
                    ToKind: CityAnchorPointKind,
                    ToId: workplaceAnchorId.Value,
                    Purpose: WorkCommutePurpose,
                    Subject: "Resident work commute",
                    MovementCapabilityIndex: ResolveMovementCapabilityIndex(
                        resident: resident,
                        purpose: WorkCommutePurpose),
                    Priority: 0,
                    OrderingKey: ResolveOrderingKey(
                        residentId: resident.Id.Value,
                        tickId: tickId,
                        salt: 101)));
        }

        private static void TryAddEmploymentReturnCandidate(
            long tickId,
            Person resident,
            ResidentialBuildingId residentialBuildingId,
            ICollection<CommuteTripCandidate> candidates)
        {
            if (resident.Employment.Status != EmploymentStatus.Employed ||
                resident.Employment.Job?.WorkplaceAnchorId is not { } workplaceAnchorId)
                return;

            candidates.Add(
                new CommuteTripCandidate(
                    TravellerEntityId: resident.Id.Value,
                    FromKind: CityAnchorPointKind,
                    FromId: workplaceAnchorId.Value,
                    ToKind: ResidentialBuildingPointKind,
                    ToId: residentialBuildingId.Value,
                    Purpose: WorkCommutePurpose,
                    Subject: "Resident work return",
                    MovementCapabilityIndex: ResolveMovementCapabilityIndex(
                        resident: resident,
                        purpose: WorkCommutePurpose),
                    Priority: 0,
                    OrderingKey: ResolveOrderingKey(
                        residentId: resident.Id.Value,
                        tickId: tickId,
                        salt: 151)));
        }

        private async Task TryAddHealthcareCandidateAsync(
            Guid cityId,
            long tickId,
            DateOnly currentDate,
            Person resident,
            ResidentialBuildingId residentialBuildingId,
            DistrictId? preferredDistrictId,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> hospitalAnchors,
            CityPopulationAnchorSelectionPolicy anchorSelectionPolicy,
            ICollection<CommuteTripCandidate> candidates,
            CancellationToken cancellationToken)
        {
            bool needsHealthcarePriority = resident.HasActiveIllness ||
                                           resident.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior;
            if (!needsHealthcarePriority)
                return;

            CityPopulationAnchorCatalogItem? primaryCareAnchor = anchorSelectionPolicy.SelectHospitalAnchor(
                anchors: hospitalAnchors,
                preferredDistrictId: preferredDistrictId,
                stableKey: resident.Id.Value);
            if (primaryCareAnchor is null)
                return;

            CityPopulationCommuteContext commute = await commuteRoutingService.ResolveHealthcareCommuteAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                healthcareAnchorId: primaryCareAnchor.CityAnchorId,
                cancellationToken: cancellationToken);
            if (!ShouldMaterializeCommuteTrip(commute))
                return;

            int priority = resident.CurrentIllnessSeverity switch
            {
                IllnessSeverity.Severe => 0,
                IllnessSeverity.Moderate => 1,
                _ => 2
            };

            candidates.Add(
                new CommuteTripCandidate(
                    TravellerEntityId: resident.Id.Value,
                    FromKind: ResidentialBuildingPointKind,
                    FromId: residentialBuildingId.Value,
                    ToKind: CityAnchorPointKind,
                    ToId: primaryCareAnchor.CityAnchorId.Value,
                    Purpose: HealthcareAccessPurpose,
                    Subject: "Resident healthcare access",
                    MovementCapabilityIndex: ResolveMovementCapabilityIndex(
                        resident: resident,
                        purpose: HealthcareAccessPurpose),
                    Priority: priority,
                    OrderingKey: ResolveOrderingKey(
                        residentId: resident.Id.Value,
                        tickId: tickId,
                        salt: 307)));
        }

        private async Task TryAddEducationCandidateAsync(
            Guid cityId,
            long tickId,
            Person resident,
            ResidentialBuildingId residentialBuildingId,
            ICollection<CommuteTripCandidate> candidates,
            CancellationToken cancellationToken)
        {
            if (resident.Employment.Status != EmploymentStatus.Student ||
                resident.Education.CurrentInstitutionAnchorId is not { } institutionAnchorId)
                return;

            CityPopulationCommuteContext commute = await commuteRoutingService.ResolveEducationCommuteAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                resident: resident,
                cancellationToken: cancellationToken);
            if (!ShouldMaterializeCommuteTrip(commute))
                return;

            candidates.Add(
                new CommuteTripCandidate(
                    TravellerEntityId: resident.Id.Value,
                    FromKind: ResidentialBuildingPointKind,
                    FromId: residentialBuildingId.Value,
                    ToKind: CityAnchorPointKind,
                    ToId: institutionAnchorId.Value,
                    Purpose: EducationCommutePurpose,
                    Subject: "Resident education commute",
                    MovementCapabilityIndex: ResolveMovementCapabilityIndex(
                        resident: resident,
                        purpose: EducationCommutePurpose),
                    Priority: 1,
                    OrderingKey: ResolveOrderingKey(
                        residentId: resident.Id.Value,
                        tickId: tickId,
                        salt: 211)));
        }

        private static void TryAddEducationReturnCandidate(
            long tickId,
            Person resident,
            ResidentialBuildingId residentialBuildingId,
            ICollection<CommuteTripCandidate> candidates)
        {
            if (resident.Employment.Status != EmploymentStatus.Student ||
                resident.Education.CurrentInstitutionAnchorId is not { } institutionAnchorId)
                return;

            candidates.Add(
                new CommuteTripCandidate(
                    TravellerEntityId: resident.Id.Value,
                    FromKind: CityAnchorPointKind,
                    FromId: institutionAnchorId.Value,
                    ToKind: ResidentialBuildingPointKind,
                    ToId: residentialBuildingId.Value,
                    Purpose: EducationCommutePurpose,
                    Subject: "Resident education return",
                    MovementCapabilityIndex: ResolveMovementCapabilityIndex(
                        resident: resident,
                        purpose: EducationCommutePurpose),
                    Priority: 1,
                    OrderingKey: ResolveOrderingKey(
                        residentId: resident.Id.Value,
                        tickId: tickId,
                        salt: 241)));
        }

        private static bool ShouldMaterializeCommuteTrip(CityPopulationCommuteContext commute)
        {
            return commute.HasRouteData &&
                   commute.IsAccessible &&
                   commute.AccessibilityIndex >= MinAccessibilityIndex;
        }

        private static decimal ResolveMovementCapabilityIndex(
            Person resident,
            string purpose)
        {
            decimal healthFactor = 0.52m + ((resident.Health.Value / 100m) * 0.58m);
            decimal energyFactor = 0.58m + ((resident.Energy.Value / 100m) * 0.52m);
            decimal stressFactor = 1.04m - ((resident.Stress.Value / 100m) * 0.22m);
            decimal illnessFactor = resident.CurrentIllnessSeverity switch
            {
                IllnessSeverity.Severe => 0.58m,
                IllnessSeverity.Moderate => 0.78m,
                IllnessSeverity.Mild => 0.90m,
                _ => 1m
            };
            decimal weightFactor = resident.Weight.Kilograms switch
            {
                > 120m => 0.88m,
                > 100m => 0.92m,
                > 85m => 0.96m,
                < 45m => 0.96m,
                _ => 1m
            };
            decimal purposeFactor = string.Equals(
                a: purpose,
                b: WorkCommutePurpose,
                comparisonType: StringComparison.Ordinal)
                ? 1.05m
                : string.Equals(
                    a: purpose,
                    b: HealthcareAccessPurpose,
                    comparisonType: StringComparison.Ordinal)
                    ? 0.93m
                : 1.00m;

            return decimal.Round(
                d: Math.Clamp(
                    value: healthFactor * energyFactor * stressFactor * illnessFactor * weightFactor * purposeFactor,
                    min: 0.35m,
                    max: 1.65m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static long ResolveOrderingKey(
            Guid residentId,
            long tickId,
            int salt)
        {
            unchecked
            {
                byte[] bytes = residentId.ToByteArray();
                long hash = 19;

                for (int i = 0; i < bytes.Length; i++)
                    hash = (hash * 37) + bytes[i];

                hash = (hash * 37) + (tickId % 997);
                hash = (hash * 37) + salt;

                return Math.Abs(hash);
            }
        }

        private static string BuildTripConcurrencyKey(
            Guid travellerEntityId,
            string purpose)
        {
            return $"{travellerEntityId:N}:{purpose.Trim().ToLowerInvariant()}";
        }

        private static MobilityPhaseWindow ResolvePhaseWindow(DateTimeOffset currentSimTimeUtc)
        {
            TimeOnly time = TimeOnly.FromDateTime(currentSimTimeUtc.UtcDateTime);

            bool shouldDispatchOutboundCommutes = time >= new TimeOnly(6, 0) && time < new TimeOnly(10, 30);
            bool shouldDispatchReturnCommutes = time >= new TimeOnly(16, 0) && time < new TimeOnly(20, 30);
            bool shouldDispatchHealthcare = time >= new TimeOnly(8, 0) && time < new TimeOnly(20, 0);

            return new MobilityPhaseWindow(
                ShouldDispatchOutboundCommutes: shouldDispatchOutboundCommutes,
                ShouldDispatchReturnCommutes: shouldDispatchReturnCommutes,
                ShouldDispatchHealthcare: shouldDispatchHealthcare);
        }

        private sealed record CommuteTripCandidate(
            Guid TravellerEntityId,
            string FromKind,
            Guid FromId,
            string ToKind,
            Guid ToId,
            string Purpose,
            string Subject,
            decimal MovementCapabilityIndex,
            int Priority,
            long OrderingKey);

        private readonly record struct MobilityPhaseWindow(
            bool ShouldDispatchOutboundCommutes,
            bool ShouldDispatchReturnCommutes,
            bool ShouldDispatchHealthcare)
        {
            public bool HasAnyDispatch =>
                ShouldDispatchOutboundCommutes ||
                ShouldDispatchReturnCommutes ||
                ShouldDispatchHealthcare;
        }
    }
}
