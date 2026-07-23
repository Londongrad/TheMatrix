using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;

namespace Matrix.Education.Domain.Students
{
    /// <summary>
    ///     Education-owned reference to a resident. Demographic facts are synchronized from Population;
    ///     Education never mutates the source resident aggregate.
    /// </summary>
    public sealed class StudentProfile : AggregateRoot<ResidentId>
    {
        private StudentProfile(
            ResidentId residentId,
            SimulationHostId simulationHostId,
            DateOnly birthDate,
            bool isAlive,
            bool isActive,
            EducationStageKey? completedStage,
            DateOnly? completedStageOn,
            long lastSourceRevision,
            long lastLifecycleRevision,
            DateTimeOffset lastSynchronizedAtUtc)
            : base(residentId)
        {
            SimulationHostId = simulationHostId;
            BirthDate = birthDate;
            IsAlive = isAlive;
            IsActive = isActive;
            CompletedStage = completedStage;
            CompletedStageOn = completedStageOn;
            LastSourceRevision = EnsureRevision(lastSourceRevision);
            LastLifecycleRevision = EnsureRevision(lastLifecycleRevision);
            ParticipationRevision = 0;
            LastSynchronizedAtUtc = EnsureUtc(lastSynchronizedAtUtc);
        }

        private StudentProfile()
            : base(default(ResidentId))
        {
        }

        public ResidentId ResidentId => Id;
        public SimulationHostId SimulationHostId { get; private set; }
        public DateOnly BirthDate { get; private set; }
        public bool IsAlive { get; private set; }
        public bool IsActive { get; private set; }
        public EducationStageKey? CompletedStage { get; private set; }
        public DateOnly? CompletedStageOn { get; private set; }
        public long LastSourceRevision { get; private set; }
        public long LastLifecycleRevision { get; private set; }
        public long ParticipationRevision { get; private set; }
        public long? LastAttendanceSourceTickId { get; private set; }
        public DateTimeOffset? AttendanceObservedAtSimTimeUtc { get; private set; }
        public decimal? AttendanceIndex { get; private set; }
        public decimal? CommuteAccessibilityIndex { get; private set; }
        public DateTimeOffset LastSynchronizedAtUtc { get; private set; }

        public static StudentProfile Register(
            ResidentId residentId,
            SimulationHostId simulationHostId,
            DateOnly birthDate,
            bool isAlive,
            bool isActive,
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc,
            long lifecycleRevision = 0)
        {
            return new StudentProfile(
                residentId: residentId,
                simulationHostId: simulationHostId,
                birthDate: birthDate,
                isAlive: isAlive,
                isActive: isActive,
                completedStage: null,
                completedStageOn: null,
                lastSourceRevision: sourceRevision,
                lastLifecycleRevision: lifecycleRevision,
                lastSynchronizedAtUtc: synchronizedAtUtc);
        }

        public bool TrySynchronizeResidentFacts(
            SimulationHostId simulationHostId,
            DateOnly birthDate,
            bool isAlive,
            bool isActive,
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc,
            long lifecycleRevision = 0)
        {
            EnsureSameSimulationHost(simulationHostId);

            long normalizedSourceRevision = EnsureRevision(sourceRevision);
            long normalizedLifecycleRevision = EnsureRevision(lifecycleRevision);
            DateTimeOffset normalizedTimestamp = EnsureUtc(synchronizedAtUtc);
            bool sourceChanged = normalizedSourceRevision > LastSourceRevision;
            bool lifecycleChanged = normalizedLifecycleRevision > LastLifecycleRevision;

            if (!sourceChanged && !lifecycleChanged)
                return false;

            if (sourceChanged)
            {
                BirthDate = birthDate;
                IsActive = isActive;
                if (!isActive)
                    ClearAttendance();
                LastSourceRevision = normalizedSourceRevision;
            }

            if (lifecycleChanged)
            {
                ClearAttendance();
                IsAlive = isAlive;
                LastLifecycleRevision = normalizedLifecycleRevision;
            }

            LastSynchronizedAtUtc = normalizedTimestamp;

            return true;
        }

        public bool TryDeactivate(
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc)
        {
            if (!TryAcceptSourceRevision(sourceRevision, synchronizedAtUtc))
                return false;

            IsActive = false;
            ClearAttendance();

            return true;
        }

        public bool TryReactivate(
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc)
        {
            if (!TryAcceptSourceRevision(sourceRevision, synchronizedAtUtc))
                return false;

            IsActive = true;

            return true;
        }

        public void RecordStageCompletion(
            EducationStageKey stage,
            DateOnly completedOn)
        {
            if (!IsAlive || !IsActive)
                throw new InvalidOperationException(
                    "An unavailable student cannot complete an education stage.");
            if (completedOn < BirthDate)
                throw new ArgumentOutOfRangeException(
                    nameof(completedOn),
                    "An education stage cannot be completed before the student's birth date.");

            CompletedStage = stage;
            CompletedStageOn = completedOn;
        }

        public long RecordParticipationChange()
        {
            ClearAttendance();
            ParticipationRevision = checked(ParticipationRevision + 1);
            return ParticipationRevision;
        }

        public bool TryRecordAttendance(long sourceTickId, long participationRevision, long lifecycleRevision,
            DateTimeOffset observedAtSimTimeUtc, decimal attendanceIndex, decimal commuteAccessibilityIndex)
        {
            EnsureRevision(sourceTickId);
            EnsureUtc(observedAtSimTimeUtc);
            if (attendanceIndex is < 0m or > 1m || commuteAccessibilityIndex is < 0m or > 2m)
                throw new ArgumentOutOfRangeException(nameof(attendanceIndex));
            if (!IsAlive || !IsActive || participationRevision != ParticipationRevision
                || lifecycleRevision != LastLifecycleRevision || sourceTickId <= LastAttendanceSourceTickId
                || observedAtSimTimeUtc < AttendanceObservedAtSimTimeUtc)
                return false;

            LastAttendanceSourceTickId = sourceTickId;
            AttendanceObservedAtSimTimeUtc = observedAtSimTimeUtc;
            AttendanceIndex = attendanceIndex;
            CommuteAccessibilityIndex = commuteAccessibilityIndex;
            return true;
        }

        private void ClearAttendance()
        {
            AttendanceObservedAtSimTimeUtc = null;
            AttendanceIndex = null;
            CommuteAccessibilityIndex = null;
        }

        private bool TryAcceptSourceRevision(
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc)
        {
            long normalizedRevision = EnsureRevision(sourceRevision);
            DateTimeOffset normalizedTimestamp = EnsureUtc(synchronizedAtUtc);

            if (normalizedRevision <= LastSourceRevision)
                return false;

            LastSourceRevision = normalizedRevision;
            LastSynchronizedAtUtc = normalizedTimestamp;

            return true;
        }

        private void EnsureSameSimulationHost(SimulationHostId simulationHostId)
        {
            if (simulationHostId != SimulationHostId)
                throw new InvalidOperationException(
                    "A synchronized student profile cannot move between simulation hosts.");
        }

        private static DateTimeOffset EnsureUtc(DateTimeOffset value)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : throw new ArgumentException(
                    message: "Synchronization timestamps must be expressed in UTC.",
                    paramName: nameof(value));
        }

        private static long EnsureRevision(long value)
        {
            return value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: "Source revisions cannot be negative.");
        }
    }
}
