using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
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
            CancellationToken cancellationToken,
            int utcOffsetMinutes = 0)
        {
            if (residents.Count == 0 || householdPlacements.Count == 0)
                return;

            DateTimeOffset localTime = currentSimTimeUtc.ToOffset(TimeSpan.FromMinutes(utcOffsetMinutes));
            MobilityPhaseWindow workWindow = ResolvePhaseWindow(localTime);
            var homesByHouseholdId = householdPlacements.GroupBy(placement => placement.HouseholdId)
                .ToDictionary(group => group.Key, group => group.Select(placement => placement.ResidentialBuildingId).FirstOrDefault());
            var windowsByRoutine = new Dictionary<PersonRoutineProfile, MobilityPhaseWindow>();
            var scheduledResidents = new List<(Person Resident, ResidentialBuildingId Home,
                ResidentExternalActivityProfile Activity, MobilityPhaseWindow Window)>();

            foreach (Person resident in residents)
            {
                if (!resident.IsAlive || !homesByHouseholdId.TryGetValue(resident.HouseholdId, out var home) || home is null)
                    continue;
                var activity = externalActivitiesByResidentId.TryGetValue(resident.Id, out var external)
                    && external.ResidentLifecycleRevision == resident.LifecycleRevision
                    ? external : ResidentExternalActivityProfile.None;
                MobilityPhaseWindow window = default;
                if (activity is { HasStructuredActivity: true, DestinationAnchorId: not null, CommutePurpose: { Length: > 0 } })
                {
                    if (!windowsByRoutine.TryGetValue(activity.Routine, out window))
                    {
                        window = ResolveExternalPhaseWindow(activity.Routine, localTime);
                        windowsByRoutine.Add(activity.Routine, window);
                    }
                }
                bool workDue = workWindow.HasAnyDispatch && resident.Employment.Status == EmploymentStatus.Employed
                    && resident.Employment.Job?.WorkplaceAnchorId is not null;
                if (workDue || window.HasAnyDispatch)
                    scheduledResidents.Add((resident, home.Value, activity, window));
            }
            if (scheduledResidents.Count == 0)
                return;

            var activeTrips = await activeTripClient.ListActiveByCityAsync(cityId, cancellationToken);
            var activeTripKeys = activeTrips.Where(trip => trip.TravellerEntityId.HasValue)
                .Select(trip => BuildTripConcurrencyKey(trip.TravellerEntityId!.Value, trip.Purpose))
                .ToHashSet(StringComparer.Ordinal);
            List<CommuteTripCandidate> candidates = [];

            foreach (var (resident, home, activity, window) in scheduledResidents)
            {
                string workTripKey = BuildTripConcurrencyKey(resident.Id.Value, WorkCommutePurpose);
                if (!activeTripKeys.Contains(workTripKey))
                {
                    if (workWindow.ShouldDispatchOutboundCommutes)
                        await TryAddEmploymentCandidateAsync(cityId, tickId, resident, home, candidates, cancellationToken);
                    else if (workWindow.ShouldDispatchReturnCommutes)
                        TryAddEmploymentReturnCandidate(tickId, resident, home, candidates);
                }

                if (activity is not { HasStructuredActivity: true, DestinationAnchorId: not null,
                        CommutePurpose: { Length: > 0 } purpose })
                    continue;
                string activityTripKey = BuildTripConcurrencyKey(resident.Id.Value, purpose);
                if (!activeTripKeys.Contains(activityTripKey))
                {
                    if (window.ShouldDispatchOutboundCommutes)
                        await TryAddExternalActivityCandidateAsync(cityId, tickId, resident, activity, home, candidates, cancellationToken);
                    else if (window.ShouldDispatchReturnCommutes)
                        TryAddExternalActivityReturnCandidate(tickId, resident, activity, home, candidates);
                }
            }

            foreach (CommuteTripCandidate candidate in candidates.OrderBy(item => item.Priority)
                         .ThenBy(item => item.OrderingKey).Take(MaxTripsPerTick))
            {
                string concurrencyKey = BuildTripConcurrencyKey(candidate.TravellerEntityId, candidate.Purpose);
                if (activeTripKeys.Contains(concurrencyKey))
                    continue;

                bool dispatched = await activeTripClient.TryDispatchAsync(
                    new CityPopulationTripDispatchRequest(
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
                    cancellationToken);
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

        private static MobilityPhaseWindow ResolveExternalPhaseWindow(PersonRoutineProfile routine, DateTimeOffset localTime)
        {
            bool outbound = false;
            bool returning = false;
            // Keep transport sampling window sizes, but anchor them to the activity schedule.
            // Adjacent dates account for departures and returns crossing local midnight.
            var localMidnight = new DateTimeOffset(localTime.Date, localTime.Offset);
            for (int dayOffset = -1; dayOffset <= 1; dayOffset++)
            {
                var date = localMidnight.AddDays(dayOffset);
                if (!routine.IsScheduledOn(date.DayOfWeek)) continue;
                var start = date.Add(routine.StructuredActivityStart!.Value);
                var end = date.Add(routine.StructuredActivityEnd!.Value);
                outbound |= localTime >= start.AddHours(-2) && localTime < start.AddMinutes(150) && localTime < end;
                returning |= localTime >= end && localTime < end.AddMinutes(270);
            }
            return new(outbound, returning);
        }

        private static MobilityPhaseWindow ResolvePhaseWindow(DateTimeOffset localTime)
        {
            var time = TimeOnly.FromDateTime(localTime.DateTime);

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
