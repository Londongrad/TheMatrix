using Matrix.BuildingBlocks.Domain.Common;
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
            long lastSourceRevision,
            DateTimeOffset lastSynchronizedAtUtc)
            : base(residentId)
        {
            SimulationHostId = simulationHostId;
            BirthDate = birthDate;
            IsAlive = isAlive;
            IsActive = isActive;
            LastSourceRevision = EnsureRevision(lastSourceRevision);
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
        public long LastSourceRevision { get; private set; }
        public DateTimeOffset LastSynchronizedAtUtc { get; private set; }

        public static StudentProfile Register(
            ResidentId residentId,
            SimulationHostId simulationHostId,
            DateOnly birthDate,
            bool isAlive,
            bool isActive,
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc)
        {
            return new StudentProfile(
                residentId: residentId,
                simulationHostId: simulationHostId,
                birthDate: birthDate,
                isAlive: isAlive,
                isActive: isActive,
                lastSourceRevision: sourceRevision,
                lastSynchronizedAtUtc: synchronizedAtUtc);
        }

        public bool TrySynchronizeResidentFacts(
            SimulationHostId simulationHostId,
            DateOnly birthDate,
            bool isAlive,
            bool isActive,
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc)
        {
            EnsureSameSimulationHost(simulationHostId);

            if (!TryAcceptSourceRevision(sourceRevision, synchronizedAtUtc))
                return false;

            BirthDate = birthDate;
            IsAlive = isAlive;
            IsActive = isActive;

            return true;
        }

        public bool TryDeactivate(
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc)
        {
            if (!TryAcceptSourceRevision(sourceRevision, synchronizedAtUtc))
                return false;

            IsActive = false;

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
