using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Integration;
using Matrix.Population.Infrastructure.Integration.Education;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Infrastructure.Persistence.Entities
{
    public sealed class EducationParticipationProjectionEntity
    {
        private EducationParticipationProjectionEntity()
        {
        }

        private EducationParticipationProjectionEntity(
            EducationParticipationProjection projection)
        {
            SimulationHostId = projection.SimulationHostId;
            ResidentId = PersonId.From(projection.ResidentId);
            Apply(projection);
        }

        public Guid SimulationHostId { get; private set; }
        public PersonId ResidentId { get; private set; }
        public long ParticipationRevision { get; private set; }
        public long ResidentLifecycleRevision { get; private set; }
        public bool IsEnrolled { get; private set; }
        public string? ActiveStage { get; private set; }
        public Guid? InstitutionId { get; private set; }
        public Guid? InstitutionAnchorId { get; private set; }
        public DateOnly? EnrolledOn { get; private set; }
        public string? CompletedStage { get; private set; }
        public DateOnly? CompletedStageOn { get; private set; }
        public DateOnly SnapshotDate { get; private set; }
        public DateTimeOffset OccurredAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }
        public string? EconomicEffectsJson { get; private set; }
        public long? AttendanceSourceTickId { get; private set; }
        public DateTimeOffset? AttendanceObservedAtSimTimeUtc { get; private set; }
        public decimal? AttendanceIndex { get; private set; }
        public decimal? CommuteAccessibilityIndex { get; private set; }

        public bool TryApplyAttendance(long sourceTickId, DateTimeOffset observedAtSimTimeUtc, EducationAttendanceInput input)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (sourceTickId < 0 || observedAtSimTimeUtc.Offset != TimeSpan.Zero
                || input.AttendanceIndex is < 0m or > 1m || input.CommuteAccessibilityIndex is < 0m or > 2m)
                throw new ArgumentException("Invalid attendance observation.");
            if (!IsEnrolled || input.ResidentId != ResidentId.Value
                || input.ParticipationRevision != ParticipationRevision || input.ResidentLifecycleRevision != ResidentLifecycleRevision
                || sourceTickId <= AttendanceSourceTickId || observedAtSimTimeUtc < AttendanceObservedAtSimTimeUtc)
                return false;
            AttendanceSourceTickId = sourceTickId;
            AttendanceObservedAtSimTimeUtc = observedAtSimTimeUtc;
            AttendanceIndex = input.AttendanceIndex;
            CommuteAccessibilityIndex = input.CommuteAccessibilityIndex;
            return true;
        }

        public static EducationParticipationProjectionEntity Create(
            EducationParticipationProjection projection)
        {
            ArgumentNullException.ThrowIfNull(projection);
            return new EducationParticipationProjectionEntity(projection);
        }

        public bool TryApply(EducationParticipationProjection projection)
        {
            ArgumentNullException.ThrowIfNull(projection);
            if (projection.SimulationHostId != SimulationHostId
                || projection.ResidentId != ResidentId.Value)
                throw new InvalidOperationException(
                    "An education participation projection cannot change its identity.");
            if (projection.ParticipationRevision <= ParticipationRevision)
                return false;

            Apply(projection);
            return true;
        }

        public EducationParticipationProjection ToProjection(Dictionary<string, ResidentExternalEconomicProfile>? economicsCache = null)
        {
            ResidentExternalEconomicProfile? economics = null;
            if (EconomicEffectsJson is { } json && (economicsCache is null || !economicsCache.TryGetValue(json, out economics)))
            {
                economics = EducationEconomicEffectsMapper.Deserialize(json);
                economicsCache?.Add(json, economics);
            }
            return new EducationParticipationProjection(
                SimulationHostId,
                ResidentId.Value,
                ParticipationRevision,
                ResidentLifecycleRevision,
                IsEnrolled,
                ActiveStage,
                InstitutionId,
                InstitutionAnchorId,
                EnrolledOn,
                CompletedStage,
                CompletedStageOn,
                SnapshotDate,
                OccurredAtUtc,
                UpdatedAtUtc,
                economics,
                AttendanceIndex is { } attendance && AttendanceObservedAtSimTimeUtc is { } observedAt
                    && AttendanceSourceTickId is { } tickId && CommuteAccessibilityIndex is { } commute
                    ? new EducationAttendanceProjection(tickId, observedAt, attendance, commute) : null);
        }

        private void Apply(EducationParticipationProjection projection)
        {
            if (projection.SimulationHostId == Guid.Empty
                || projection.ResidentId == Guid.Empty
                || projection.ParticipationRevision <= 0
                || projection.ResidentLifecycleRevision < 0)
                throw new ArgumentException("Education participation projection identity is invalid.");
            if (projection.OccurredAtUtc.Offset != TimeSpan.Zero
                || projection.UpdatedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("Education participation timestamps must be UTC.");

            ParticipationRevision = projection.ParticipationRevision;
            ResidentLifecycleRevision = projection.ResidentLifecycleRevision;
            IsEnrolled = projection.IsEnrolled;
            ActiveStage = projection.ActiveStage;
            InstitutionId = projection.InstitutionId;
            InstitutionAnchorId = projection.InstitutionAnchorId;
            EnrolledOn = projection.EnrolledOn;
            CompletedStage = projection.CompletedStage;
            CompletedStageOn = projection.CompletedStageOn;
            SnapshotDate = projection.SnapshotDate;
            OccurredAtUtc = projection.OccurredAtUtc;
            UpdatedAtUtc = projection.UpdatedAtUtc;
            EconomicEffectsJson = projection.Economics is null ? null : EducationEconomicEffectsMapper.Serialize(projection.Economics);
            AttendanceObservedAtSimTimeUtc = null;
            AttendanceIndex = null;
            CommuteAccessibilityIndex = null;
        }
    }
}
