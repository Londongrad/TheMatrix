using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
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
        private const string WorkCommutePurpose = "WorkCommute";

        public async Task SyncAsync(
            Guid cityId,
            long tickId,
            DateTimeOffset currentSimTimeUtc,
            IReadOnlyCollection<Person> residents,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            IReadOnlyDictionary<PersonId, ResidentExternalActivityProfile> externalActivitiesByResidentId,
            CancellationToken cancellationToken)
        {
            if (residents.Count == 0 || householdPlacements.Count == 0)
                return;

            MobilityPhaseWindow phaseWindow = ResolvePhaseWindow(currentSimTimeUtc);
            if (!phaseWindow.HasAnyDispatch)
                return;

            Person[] aliveResidents = residents
               .Where(resident => resident.IsAlive)
               .ToArray();
            if (aliveResidents.Length == 0)
                return;

            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId =
                householdPlacements
                   .GroupBy(x => x.HouseholdId)
                   .ToDictionary(
                        keySelector: x => x.Key,
                        elementSelector: x => x.Select(y => y.ResidentialBuildingId)
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

            foreach (Person resident in aliveResidents)
            {
                if (!residentialBuildingByHouseholdId.TryGetValue(
                        key: resident.HouseholdId,
                        value: out ResidentialBuildingId? residentialBuildingId) ||
                    !residentialBuildingId.HasValue)
                    continue;

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
                else
                    if (phaseWindow.ShouldDispatchReturnCommutes &&
                        !activeTripKeys.Contains(workTripKey))
                    TryAddEmploymentReturnCandidate(
                        tickId: tickId,
                        resident: resident,
                        residentialBuildingId: residentialBuildingId.Value,
                        candidates: candidates);

                ResidentExternalActivityProfile externalActivity =
                    externalActivitiesByResidentId.TryGetValue(
                        key: resident.Id,
                        value: out ResidentExternalActivityProfile? resolvedActivity) &&
                    resolvedActivity.ResidentLifecycleRevision == resident.LifecycleRevision
                        ? resolvedActivity
                        : ResidentExternalActivityProfile.None;
                if (externalActivity is not
                    {
                        HasStructuredActivity: true,
                        DestinationAnchorId: not null,
                        CommutePurpose: { Length: > 0 } externalActivityPurpose
                    })
                    continue;

                string externalActivityTripKey = BuildTripConcurrencyKey(
                    travellerEntityId: resident.Id.Value,
                    purpose: externalActivityPurpose);
                if (phaseWindow.ShouldDispatchOutboundCommutes &&
                    !activeTripKeys.Contains(externalActivityTripKey))
                    await TryAddExternalActivityCandidateAsync(
                        cityId: cityId,
                        tickId: tickId,
                        resident: resident,
                        externalActivity: externalActivity,
                        residentialBuildingId: residentialBuildingId.Value,
                        candidates: candidates,
                        cancellationToken: cancellationToken);
                else
                    if (phaseWindow.ShouldDispatchReturnCommutes &&
                        !activeTripKeys.Contains(externalActivityTripKey))
                    TryAddExternalActivityReturnCandidate(
                        tickId: tickId,
                        resident: resident,
                        externalActivity: externalActivity,
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
                resident.Employment.Job?.WorkplaceAnchorId is not
                { } workplaceAnchorId)
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
                resident.Employment.Job?.WorkplaceAnchorId is not
                { } workplaceAnchorId)
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

        private async Task TryAddExternalActivityCandidateAsync(
            Guid cityId,
            long tickId,
            Person resident,
            ResidentExternalActivityProfile externalActivity,
            ResidentialBuildingId residentialBuildingId,
            ICollection<CommuteTripCandidate> candidates,
            CancellationToken cancellationToken)
        {
            if (externalActivity is not
                {
                    HasStructuredActivity: true,
                    DestinationAnchorId: { } destinationAnchorId,
                    CommutePurpose: { Length: > 0 } commutePurpose
                })
                return;

            CityPopulationCommuteContext commute = await commuteRoutingService.ResolveAnchorCommuteAsync(
                cityId: cityId,
                residentialBuildingId: residentialBuildingId,
                destinationAnchorId: CityAnchorId.From(destinationAnchorId),
                cancellationToken: cancellationToken);
            if (!ShouldMaterializeCommuteTrip(commute))
                return;

            candidates.Add(
                new CommuteTripCandidate(
                    TravellerEntityId: resident.Id.Value,
                    FromKind: ResidentialBuildingPointKind,
                    FromId: residentialBuildingId.Value,
                    ToKind: CityAnchorPointKind,
                    ToId: destinationAnchorId,
                    Purpose: commutePurpose,
                    Subject: "Resident external activity commute",
                    MovementCapabilityIndex: ResolveMovementCapabilityIndex(
                        resident: resident,
                        purpose: commutePurpose),
                    Priority: 1,
                    OrderingKey: ResolveOrderingKey(
                        residentId: resident.Id.Value,
                        tickId: tickId,
                        salt: 211)));
        }

        private static void TryAddExternalActivityReturnCandidate(
            long tickId,
            Person resident,
            ResidentExternalActivityProfile externalActivity,
            ResidentialBuildingId residentialBuildingId,
            ICollection<CommuteTripCandidate> candidates)
        {
            if (externalActivity is not
                {
                    HasStructuredActivity: true,
                    DestinationAnchorId: { } destinationAnchorId,
                    CommutePurpose: { Length: > 0 } commutePurpose
                })
                return;

            candidates.Add(
                new CommuteTripCandidate(
                    TravellerEntityId: resident.Id.Value,
                    FromKind: CityAnchorPointKind,
                    FromId: destinationAnchorId,
                    ToKind: ResidentialBuildingPointKind,
                    ToId: residentialBuildingId.Value,
                    Purpose: commutePurpose,
                    Subject: "Resident external activity return",
                    MovementCapabilityIndex: ResolveMovementCapabilityIndex(
                        resident: resident,
                        purpose: commutePurpose),
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
            decimal healthFactor = 0.52m + (resident.Health.Value / 100m * 0.58m);
            decimal energyFactor = 0.58m + (resident.Energy.Value / 100m * 0.52m);
            decimal stressFactor = 1.04m - (resident.Stress.Value / 100m * 0.22m);
            decimal functionalCapacityFactor = 0.55m +
                                               (resident.FunctionalCapacity.Value / 100m * 0.45m);
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
                : 1.00m;

            return decimal.Round(
                d: Math.Clamp(
                    value: healthFactor * energyFactor * stressFactor * functionalCapacityFactor * weightFactor *
                           purposeFactor,
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
            var time = TimeOnly.FromDateTime(currentSimTimeUtc.UtcDateTime);

            bool shouldDispatchOutboundCommutes = time >=
                                                  new TimeOnly(
                                                      hour: 6,
                                                      minute: 0) &&
                                                  time <
                                                  new TimeOnly(
                                                      hour: 10,
                                                      minute: 30);
            bool shouldDispatchReturnCommutes = time >=
                                                new TimeOnly(
                                                    hour: 16,
                                                    minute: 0) &&
                                                time <
                                                new TimeOnly(
                                                    hour: 20,
                                                    minute: 30);
            return new MobilityPhaseWindow(
                ShouldDispatchOutboundCommutes: shouldDispatchOutboundCommutes,
                ShouldDispatchReturnCommutes: shouldDispatchReturnCommutes);
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
            bool ShouldDispatchReturnCommutes)
        {
            public bool HasAnyDispatch =>
                ShouldDispatchOutboundCommutes ||
                ShouldDispatchReturnCommutes;
        }
    }
}
