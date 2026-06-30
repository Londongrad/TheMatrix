using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Domain.Patients
{
    /// <summary>
    ///     Healthcare-owned reference to a Population resident. Demographic facts are replicated for
    ///     medical decisions; Healthcare never mutates the source resident aggregate.
    /// </summary>
    public sealed class PatientProfile : AggregateRoot<PatientId>
    {
        private PatientProfile(
            PatientId patientId,
            SimulationHostId simulationHostId,
            DateOnly birthDate,
            PatientSex sex,
            bool isAlive,
            bool isActive,
            long lastSourceRevision,
            long lastLifecycleRevision,
            DateTimeOffset lastSynchronizedAtUtc)
            : base(patientId)
        {
            SimulationHostId = simulationHostId;
            BirthDate = birthDate;
            Sex = EnsureSex(sex);
            IsAlive = isAlive;
            IsActive = isActive;
            LastSourceRevision = EnsureRevision(lastSourceRevision);
            LastLifecycleRevision = EnsureRevision(lastLifecycleRevision);
            LastSynchronizedAtUtc = EnsureUtc(lastSynchronizedAtUtc);
        }

        private PatientProfile()
            : base(default(PatientId))
        {
        }

        public PatientId PatientId => Id;
        public SimulationHostId SimulationHostId { get; private set; }
        public DateOnly BirthDate { get; private set; }
        public PatientSex Sex { get; private set; }
        public bool IsAlive { get; private set; }
        public bool IsActive { get; private set; }
        public long LastSourceRevision { get; private set; }
        public long LastLifecycleRevision { get; private set; }
        public DateTimeOffset LastSynchronizedAtUtc { get; private set; }

        public bool IsEligibleForCare => IsAlive && IsActive;

        public static PatientProfile Register(
            PatientId patientId,
            SimulationHostId simulationHostId,
            DateOnly birthDate,
            PatientSex sex,
            bool isAlive,
            bool isActive,
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc,
            long lifecycleRevision = 0)
        {
            return new PatientProfile(
                patientId: patientId,
                simulationHostId: simulationHostId,
                birthDate: birthDate,
                sex: sex,
                isAlive: isAlive,
                isActive: isActive,
                lastSourceRevision: sourceRevision,
                lastLifecycleRevision: lifecycleRevision,
                lastSynchronizedAtUtc: synchronizedAtUtc);
        }

        public bool TrySynchronizeResidentFacts(
            SimulationHostId simulationHostId,
            DateOnly birthDate,
            PatientSex sex,
            bool isAlive,
            bool isActive,
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc,
            long lifecycleRevision = 0)
        {
            EnsureSameSimulationHost(simulationHostId);

            long normalizedRevision = EnsureRevision(sourceRevision);
            long normalizedLifecycleRevision = EnsureRevision(lifecycleRevision);
            DateTimeOffset normalizedTimestamp = EnsureUtc(synchronizedAtUtc);
            bool sourceChanged = normalizedRevision > LastSourceRevision;
            bool lifecycleChanged = normalizedLifecycleRevision > LastLifecycleRevision;

            if (!sourceChanged && !lifecycleChanged)
                return false;

            if (sourceChanged)
            {
                BirthDate = birthDate;
                Sex = EnsureSex(sex);
                IsActive = isActive;
                LastSourceRevision = normalizedRevision;
            }

            if (lifecycleChanged)
            {
                IsAlive = isAlive;
                LastLifecycleRevision = normalizedLifecycleRevision;
            }

            LastSynchronizedAtUtc = normalizedTimestamp;

            return true;
        }

        private void EnsureSameSimulationHost(SimulationHostId simulationHostId)
        {
            if (simulationHostId != SimulationHostId)
                throw new InvalidOperationException(
                    "A synchronized patient profile cannot move between simulation hosts.");
        }

        private static PatientSex EnsureSex(PatientSex value)
        {
            return Enum.IsDefined(value)
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value));
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
