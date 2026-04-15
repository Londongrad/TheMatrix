using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World
{
    public sealed class CityActiveTrip : AggregateRoot<CityActiveTripId>
    {
        public const int MaxSubjectLength = 160;
        public const decimal MovementCapabilityIndexMin = 0.35m;
        public const decimal MovementCapabilityIndexMax = 1.65m;

        private readonly List<CityActiveTripSegment> _segments = [];

        private CityActiveTrip(
            CityActiveTripId id,
            CityId cityId,
            Guid? travellerEntityId,
            string subject,
            CityTripPurpose purpose,
            string profile,
            decimal movementCapabilityIndex,
            bool usedDynamicRoadConditions,
            long plannedAtTickId,
            long? conditionsEffectiveTickId,
            DateTimeOffset startedAtSimTimeUtc,
            DateTimeOffset lastAdvancedAtSimTimeUtc,
            DateTimeOffset expectedArrivalAtSimTimeUtc,
            DateTimeOffset? arrivedAtSimTimeUtc,
            long lastAdvancedTickId,
            decimal totalDistanceMeters,
            decimal plannedTravelTimeMinutes,
            decimal adjustedTravelTimeMinutes,
            decimal progressIndex,
            decimal distanceTravelledMeters,
            string fromKind,
            Guid fromEntityId,
            DistrictId fromDistrictId,
            RoadNodeId fromRoadNodeId,
            string fromName,
            decimal fromPositionX,
            decimal fromPositionY,
            string toKind,
            Guid toEntityId,
            DistrictId toDistrictId,
            RoadNodeId toRoadNodeId,
            string toName,
            decimal toPositionX,
            decimal toPositionY,
            DistrictId currentDistrictId,
            RoadSegmentId? currentRoadSegmentId,
            decimal currentSegmentProgressIndex,
            decimal currentPositionX,
            decimal currentPositionY,
            CityActiveTripStatus status,
            IReadOnlyCollection<CityActiveTripSegment> segments)
            : base(id)
        {
            EnsureUtc(startedAtSimTimeUtc);
            EnsureUtc(lastAdvancedAtSimTimeUtc);
            EnsureUtc(expectedArrivalAtSimTimeUtc);
            EnsureUtc(arrivedAtSimTimeUtc);

            CityId = cityId;
            TravellerEntityId = travellerEntityId;
            Subject = NormalizeSubject(subject);
            Purpose = GuardHelper.AgainstInvalidEnum(
                value: purpose,
                propertyName: nameof(Purpose));
            Profile = NormalizeProfile(profile);
            MovementCapabilityIndex = GuardHelper.AgainstOutOfRange(
                value: movementCapabilityIndex,
                min: MovementCapabilityIndexMin,
                max: MovementCapabilityIndexMax,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripCapabilityOutOfRange,
                propertyName: nameof(MovementCapabilityIndex));
            UsedDynamicRoadConditions = usedDynamicRoadConditions;
            PlannedAtTickId = plannedAtTickId;
            ConditionsEffectiveTickId = conditionsEffectiveTickId;
            StartedAtSimTimeUtc = startedAtSimTimeUtc;
            LastAdvancedAtSimTimeUtc = lastAdvancedAtSimTimeUtc;
            ExpectedArrivalAtSimTimeUtc = expectedArrivalAtSimTimeUtc;
            ArrivedAtSimTimeUtc = arrivedAtSimTimeUtc;
            LastAdvancedTickId = lastAdvancedTickId;
            TotalDistanceMeters = NormalizeDistance(totalDistanceMeters, nameof(TotalDistanceMeters));
            PlannedTravelTimeMinutes = NormalizeTravelTime(
                plannedTravelTimeMinutes,
                nameof(PlannedTravelTimeMinutes));
            AdjustedTravelTimeMinutes = NormalizeTravelTime(
                adjustedTravelTimeMinutes,
                nameof(AdjustedTravelTimeMinutes));
            ProgressIndex = NormalizeProgress(progressIndex, nameof(ProgressIndex));
            DistanceTravelledMeters = NormalizeDistance(
                distanceTravelledMeters,
                nameof(DistanceTravelledMeters));
            FromKind = NormalizePointKind(fromKind, nameof(FromKind));
            FromEntityId = GuardHelper.AgainstEmptyGuid(
                id: fromEntityId,
                propertyName: nameof(FromEntityId));
            FromDistrictId = fromDistrictId;
            FromRoadNodeId = fromRoadNodeId;
            FromName = NormalizePointName(fromName, nameof(FromName));
            FromPositionX = TopologyMapRules.NormalizeCoordinate(
                value: fromPositionX,
                propertyName: nameof(FromPositionX));
            FromPositionY = TopologyMapRules.NormalizeCoordinate(
                value: fromPositionY,
                propertyName: nameof(FromPositionY));
            ToKind = NormalizePointKind(toKind, nameof(ToKind));
            ToEntityId = GuardHelper.AgainstEmptyGuid(
                id: toEntityId,
                propertyName: nameof(ToEntityId));
            ToDistrictId = toDistrictId;
            ToRoadNodeId = toRoadNodeId;
            ToName = NormalizePointName(toName, nameof(ToName));
            ToPositionX = TopologyMapRules.NormalizeCoordinate(
                value: toPositionX,
                propertyName: nameof(ToPositionX));
            ToPositionY = TopologyMapRules.NormalizeCoordinate(
                value: toPositionY,
                propertyName: nameof(ToPositionY));
            CurrentDistrictId = currentDistrictId;
            CurrentRoadSegmentId = currentRoadSegmentId;
            CurrentSegmentProgressIndex = NormalizeProgress(
                currentSegmentProgressIndex,
                nameof(CurrentSegmentProgressIndex));
            CurrentPositionX = TopologyMapRules.NormalizeCoordinate(
                value: currentPositionX,
                propertyName: nameof(CurrentPositionX));
            CurrentPositionY = TopologyMapRules.NormalizeCoordinate(
                value: currentPositionY,
                propertyName: nameof(CurrentPositionY));
            Status = GuardHelper.AgainstInvalidEnum(
                value: status,
                propertyName: nameof(Status));

            _segments = segments
               .OrderBy(x => x.Sequence)
               .ToList();
        }

        private CityActiveTrip()
            : base(default(CityActiveTripId))
        {
            Subject = string.Empty;
            Profile = string.Empty;
            FromKind = string.Empty;
            FromName = string.Empty;
            ToKind = string.Empty;
            ToName = string.Empty;
        }

        public CityId CityId { get; private set; }
        public Guid? TravellerEntityId { get; private set; }
        public string Subject { get; private set; }
        public CityTripPurpose Purpose { get; private set; }
        public string Profile { get; private set; }
        public decimal MovementCapabilityIndex { get; private set; }
        public bool UsedDynamicRoadConditions { get; private set; }
        public long PlannedAtTickId { get; private set; }
        public long? ConditionsEffectiveTickId { get; private set; }
        public DateTimeOffset StartedAtSimTimeUtc { get; private set; }
        public DateTimeOffset LastAdvancedAtSimTimeUtc { get; private set; }
        public DateTimeOffset ExpectedArrivalAtSimTimeUtc { get; private set; }
        public DateTimeOffset? ArrivedAtSimTimeUtc { get; private set; }
        public long LastAdvancedTickId { get; private set; }
        public decimal TotalDistanceMeters { get; private set; }
        public decimal PlannedTravelTimeMinutes { get; private set; }
        public decimal AdjustedTravelTimeMinutes { get; private set; }
        public decimal ProgressIndex { get; private set; }
        public decimal DistanceTravelledMeters { get; private set; }
        public decimal RemainingDistanceMeters => decimal.Round(
            d: Math.Max(0m, TotalDistanceMeters - DistanceTravelledMeters),
            decimals: 2,
            mode: MidpointRounding.AwayFromZero);
        public string FromKind { get; private set; }
        public Guid FromEntityId { get; private set; }
        public DistrictId FromDistrictId { get; private set; }
        public RoadNodeId FromRoadNodeId { get; private set; }
        public string FromName { get; private set; }
        public decimal FromPositionX { get; private set; }
        public decimal FromPositionY { get; private set; }
        public string ToKind { get; private set; }
        public Guid ToEntityId { get; private set; }
        public DistrictId ToDistrictId { get; private set; }
        public RoadNodeId ToRoadNodeId { get; private set; }
        public string ToName { get; private set; }
        public decimal ToPositionX { get; private set; }
        public decimal ToPositionY { get; private set; }
        public DistrictId CurrentDistrictId { get; private set; }
        public RoadSegmentId? CurrentRoadSegmentId { get; private set; }
        public decimal CurrentSegmentProgressIndex { get; private set; }
        public decimal CurrentPositionX { get; private set; }
        public decimal CurrentPositionY { get; private set; }
        public CityActiveTripStatus Status { get; private set; }
        public IReadOnlyCollection<CityActiveTripSegment> Segments => _segments;

        public bool IsActive => Status == CityActiveTripStatus.Active;

        public static CityActiveTrip Create(
            CityId cityId,
            Guid? travellerEntityId,
            string subject,
            CityTripPurpose purpose,
            string profile,
            decimal movementCapabilityIndex,
            bool usedDynamicRoadConditions,
            long plannedAtTickId,
            long? conditionsEffectiveTickId,
            DateTimeOffset startedAtSimTimeUtc,
            string fromKind,
            Guid fromEntityId,
            DistrictId fromDistrictId,
            RoadNodeId fromRoadNodeId,
            string fromName,
            decimal fromPositionX,
            decimal fromPositionY,
            string toKind,
            Guid toEntityId,
            DistrictId toDistrictId,
            RoadNodeId toRoadNodeId,
            string toName,
            decimal toPositionX,
            decimal toPositionY,
            decimal totalDistanceMeters,
            decimal plannedTravelTimeMinutes,
            IReadOnlyCollection<CityActiveTripSegment> segments)
        {
            EnsureUtc(startedAtSimTimeUtc);

            decimal normalizedDistance = NormalizeDistance(
                totalDistanceMeters,
                nameof(totalDistanceMeters));
            decimal normalizedPlannedTravelTime = NormalizeTravelTime(
                plannedTravelTimeMinutes,
                nameof(plannedTravelTimeMinutes));
            decimal adjustedTravelTimeMinutes = ResolveAdjustedTravelTimeMinutes(
                plannedTravelTimeMinutes: normalizedPlannedTravelTime,
                movementCapabilityIndex: movementCapabilityIndex,
                purpose: purpose);
            bool arrivesImmediately = normalizedDistance <= 0m
                || adjustedTravelTimeMinutes <= 0.01m
                || segments.Count == 0;
            DateTimeOffset expectedArrivalAtSimTimeUtc = startedAtSimTimeUtc.AddMinutes(
                minutes: (double)Math.Max(0.01m, adjustedTravelTimeMinutes));

            return new CityActiveTrip(
                id: CityActiveTripId.New(),
                cityId: cityId,
                travellerEntityId: travellerEntityId,
                subject: subject,
                purpose: purpose,
                profile: profile,
                movementCapabilityIndex: movementCapabilityIndex,
                usedDynamicRoadConditions: usedDynamicRoadConditions,
                plannedAtTickId: plannedAtTickId,
                conditionsEffectiveTickId: conditionsEffectiveTickId,
                startedAtSimTimeUtc: startedAtSimTimeUtc,
                lastAdvancedAtSimTimeUtc: startedAtSimTimeUtc,
                expectedArrivalAtSimTimeUtc: expectedArrivalAtSimTimeUtc,
                arrivedAtSimTimeUtc: arrivesImmediately
                    ? startedAtSimTimeUtc
                    : null,
                lastAdvancedTickId: plannedAtTickId,
                totalDistanceMeters: normalizedDistance,
                plannedTravelTimeMinutes: normalizedPlannedTravelTime,
                adjustedTravelTimeMinutes: adjustedTravelTimeMinutes,
                progressIndex: arrivesImmediately
                    ? 1m
                    : 0m,
                distanceTravelledMeters: arrivesImmediately
                    ? normalizedDistance
                    : 0m,
                fromKind: fromKind,
                fromEntityId: fromEntityId,
                fromDistrictId: fromDistrictId,
                fromRoadNodeId: fromRoadNodeId,
                fromName: fromName,
                fromPositionX: fromPositionX,
                fromPositionY: fromPositionY,
                toKind: toKind,
                toEntityId: toEntityId,
                toDistrictId: toDistrictId,
                toRoadNodeId: toRoadNodeId,
                toName: toName,
                toPositionX: toPositionX,
                toPositionY: toPositionY,
                currentDistrictId: arrivesImmediately
                    ? toDistrictId
                    : fromDistrictId,
                currentRoadSegmentId: arrivesImmediately
                    ? null
                    : segments.OrderBy(x => x.Sequence).First().RoadSegmentId,
                currentSegmentProgressIndex: arrivesImmediately
                    ? 1m
                    : 0m,
                currentPositionX: arrivesImmediately
                    ? toPositionX
                    : fromPositionX,
                currentPositionY: arrivesImmediately
                    ? toPositionY
                    : fromPositionY,
                status: arrivesImmediately
                    ? CityActiveTripStatus.Arrived
                    : CityActiveTripStatus.Active,
                segments: segments);
        }

        public void AdvanceTo(
            DateTimeOffset toSimTimeUtc,
            long tickId)
        {
            EnsureUtc(toSimTimeUtc);

            if (!IsActive || toSimTimeUtc <= LastAdvancedAtSimTimeUtc)
                return;

            decimal deltaMinutes = (decimal)(toSimTimeUtc - LastAdvancedAtSimTimeUtc).TotalMinutes;

            if (deltaMinutes <= 0m)
                return;

            decimal nextProgress = NormalizeProgress(
                value: ProgressIndex + (deltaMinutes / AdjustedTravelTimeMinutes),
                propertyName: nameof(ProgressIndex));

            ProgressIndex = nextProgress;
            DistanceTravelledMeters = decimal.Round(
                d: TotalDistanceMeters * ProgressIndex,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            LastAdvancedAtSimTimeUtc = toSimTimeUtc;
            LastAdvancedTickId = tickId;

            ApplyCurrentPosition();

            if (ProgressIndex >= 0.9999m)
            {
                Status = CityActiveTripStatus.Arrived;
                ArrivedAtSimTimeUtc = toSimTimeUtc;
                CurrentDistrictId = ToDistrictId;
                CurrentRoadSegmentId = null;
                CurrentSegmentProgressIndex = 1m;
                CurrentPositionX = ToPositionX;
                CurrentPositionY = ToPositionY;
                DistanceTravelledMeters = TotalDistanceMeters;
                ProgressIndex = 1m;
            }
        }

        private void ApplyCurrentPosition()
        {
            if (_segments.Count == 0 || ProgressIndex <= 0m)
            {
                CurrentDistrictId = FromDistrictId;
                CurrentRoadSegmentId = _segments.Count == 0
                    ? null
                    : _segments[0].RoadSegmentId;
                CurrentSegmentProgressIndex = 0m;
                CurrentPositionX = FromPositionX;
                CurrentPositionY = FromPositionY;
                return;
            }

            if (ProgressIndex >= 0.9999m)
            {
                CurrentDistrictId = ToDistrictId;
                CurrentRoadSegmentId = null;
                CurrentSegmentProgressIndex = 1m;
                CurrentPositionX = ToPositionX;
                CurrentPositionY = ToPositionY;
                return;
            }

            decimal totalSegmentTravelTime = _segments.Sum(x => x.EstimatedTraversalMinutes);

            if (totalSegmentTravelTime <= 0m)
            {
                CurrentDistrictId = ToDistrictId;
                CurrentRoadSegmentId = null;
                CurrentSegmentProgressIndex = 1m;
                CurrentPositionX = ToPositionX;
                CurrentPositionY = ToPositionY;
                return;
            }

            decimal travelledRouteMinutes = totalSegmentTravelTime * ProgressIndex;
            decimal cumulativeMinutes = 0m;

            foreach (CityActiveTripSegment segment in _segments.OrderBy(x => x.Sequence))
            {
                decimal segmentEnd = cumulativeMinutes + segment.EstimatedTraversalMinutes;

                if (travelledRouteMinutes <= segmentEnd || ReferenceEquals(segment, _segments[^1]))
                {
                    decimal localProgress = segment.EstimatedTraversalMinutes <= 0m
                        ? 1m
                        : Math.Clamp(
                            value: (travelledRouteMinutes - cumulativeMinutes) / segment.EstimatedTraversalMinutes,
                            min: 0m,
                            max: 1m);

                    CurrentDistrictId = segment.DistrictId;
                    CurrentRoadSegmentId = segment.RoadSegmentId;
                    CurrentSegmentProgressIndex = decimal.Round(
                        d: localProgress,
                        decimals: 4,
                        mode: MidpointRounding.AwayFromZero);
                    CurrentPositionX = decimal.Round(
                        d: segment.FromPositionX + ((segment.ToPositionX - segment.FromPositionX) * localProgress),
                        decimals: 4,
                        mode: MidpointRounding.AwayFromZero);
                    CurrentPositionY = decimal.Round(
                        d: segment.FromPositionY + ((segment.ToPositionY - segment.FromPositionY) * localProgress),
                        decimals: 4,
                        mode: MidpointRounding.AwayFromZero);
                    return;
                }

                cumulativeMinutes = segmentEnd;
            }
        }

        private static decimal ResolveAdjustedTravelTimeMinutes(
            decimal plannedTravelTimeMinutes,
            decimal movementCapabilityIndex,
            CityTripPurpose purpose)
        {
            decimal purposeSpeedFactor = purpose switch
            {
                CityTripPurpose.WorkCommute => 1.06m,
                CityTripPurpose.EducationCommute => 0.98m,
                CityTripPurpose.HealthcareAccess => 1.02m,
                CityTripPurpose.LeisureWalk => 0.88m,
                CityTripPurpose.ServiceResponse => 1.14m,
                CityTripPurpose.HouseholdRelocation => 0.92m,
                _ => 1m
            };

            decimal effectiveSpeedFactor = Math.Max(
                0.15m,
                movementCapabilityIndex * purposeSpeedFactor);

            return decimal.Round(
                d: Math.Max(0.01m, plannedTravelTimeMinutes / effectiveSpeedFactor),
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
        }

        private static string NormalizeSubject(string value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripSubjectNullOrEmpty,
                trim: true,
                propertyName: nameof(Subject));

            if (normalized.Length > MaxSubjectLength)
                throw ClassicCityDomainErrorsFactory.CityActiveTripSubjectTooLong(
                    value: normalized,
                    max: MaxSubjectLength,
                    propertyName: nameof(Subject));

            return normalized;
        }

        private static string NormalizeProfile(string value)
        {
            return GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripProfileNullOrEmpty,
                trim: true,
                propertyName: nameof(Profile));
        }

        private static string NormalizePointKind(
            string value,
            string propertyName)
        {
            return GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripPointKindNullOrEmpty,
                trim: true,
                propertyName: propertyName);
        }

        private static string NormalizePointName(
            string value,
            string propertyName)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripPointNameNullOrEmpty,
                trim: true,
                propertyName: propertyName);

            if (normalized.Length > MaxSubjectLength)
                throw ClassicCityDomainErrorsFactory.CityActiveTripPointNameTooLong(
                    value: normalized,
                    max: MaxSubjectLength,
                    propertyName: propertyName);

            return normalized;
        }

        private static decimal NormalizeDistance(
            decimal value,
            string propertyName)
        {
            return decimal.Round(
                d: GuardHelper.AgainstOutOfRange(
                    value: value,
                    min: 0m,
                    max: 5_000_000m,
                    errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripDistanceOutOfRange,
                    propertyName: propertyName),
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal NormalizeTravelTime(
            decimal value,
            string propertyName)
        {
            return decimal.Round(
                d: GuardHelper.AgainstOutOfRange(
                    value: value,
                    min: 0.01m,
                    max: 100_000m,
                    errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripTravelTimeOutOfRange,
                    propertyName: propertyName),
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal NormalizeProgress(
            decimal value,
            string propertyName)
        {
            return decimal.Round(
                d: Math.Clamp(
                    value: GuardHelper.AgainstOutOfRange(
                        value: value,
                        min: 0m,
                        max: 1m,
                        errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripProgressOutOfRange,
                        propertyName: propertyName),
                    min: 0m,
                    max: 1m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static void EnsureUtc(DateTimeOffset value)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.CityActiveTripTimestampMustBeUtc);
        }

        private static void EnsureUtc(DateTimeOffset? value)
        {
            if (!value.HasValue)
                return;

            EnsureUtc(value.Value);
        }
    }
}
