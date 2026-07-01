using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Domain.Facilities
{
    public sealed class CareFacility : AggregateRoot<CareFacilityId>
    {
        public const int MaxNameLength = 200;

        private CareFacility(
            CareFacilityId id,
            SimulationHostId simulationHostId,
            string name,
            CareFacilityKindKey kind,
            LocationAnchorId? locationAnchorId,
            int dailyPatientCapacity,
            bool isActive,
            long lastSourceRevision,
            DateTimeOffset lastSynchronizedAtUtc)
            : base(id)
        {
            SimulationHostId = simulationHostId;
            Name = EnsureName(name);
            Kind = kind;
            LocationAnchorId = locationAnchorId;
            DailyPatientCapacity = EnsureCapacity(dailyPatientCapacity);
            IsActive = isActive;
            LastSourceRevision = EnsureRevision(lastSourceRevision);
            LastSynchronizedAtUtc = EnsureUtc(lastSynchronizedAtUtc);
        }

        private CareFacility()
            : base(default(CareFacilityId))
        {
        }

        public CareFacilityId CareFacilityId => Id;
        public SimulationHostId SimulationHostId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public CareFacilityKindKey Kind { get; private set; }
        public LocationAnchorId? LocationAnchorId { get; private set; }
        public int DailyPatientCapacity { get; private set; }
        public bool IsActive { get; private set; }
        public long LastSourceRevision { get; private set; }
        public DateTimeOffset LastSynchronizedAtUtc { get; private set; }

        public static CareFacility Register(
            CareFacilityId id,
            SimulationHostId simulationHostId,
            string name,
            CareFacilityKindKey kind,
            LocationAnchorId? locationAnchorId,
            int dailyPatientCapacity,
            bool isActive,
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc)
        {
            return new CareFacility(
                id: id,
                simulationHostId: simulationHostId,
                name: name,
                kind: kind,
                locationAnchorId: locationAnchorId,
                dailyPatientCapacity: dailyPatientCapacity,
                isActive: isActive,
                lastSourceRevision: sourceRevision,
                lastSynchronizedAtUtc: synchronizedAtUtc);
        }

        public bool TrySynchronizeProvisioning(
            SimulationHostId simulationHostId,
            string name,
            CareFacilityKindKey kind,
            LocationAnchorId? locationAnchorId,
            int dailyPatientCapacity,
            bool isActive,
            long sourceRevision,
            DateTimeOffset synchronizedAtUtc)
        {
            EnsureSameSimulationHost(simulationHostId);

            long normalizedRevision = EnsureRevision(sourceRevision);
            DateTimeOffset normalizedTimestamp = EnsureUtc(synchronizedAtUtc);
            if (normalizedRevision <= LastSourceRevision)
                return false;

            Name = EnsureName(name);
            Kind = kind;
            LocationAnchorId = locationAnchorId;
            DailyPatientCapacity = EnsureCapacity(dailyPatientCapacity);
            IsActive = isActive;
            LastSourceRevision = normalizedRevision;
            LastSynchronizedAtUtc = normalizedTimestamp;

            return true;
        }

        private void EnsureSameSimulationHost(SimulationHostId simulationHostId)
        {
            if (simulationHostId != SimulationHostId)
                throw new InvalidOperationException(
                    "A care facility cannot move between simulation hosts.");
        }

        private static string EnsureName(string? value)
        {
            string normalized = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException(
                    message: "A care facility name is required.",
                    paramName: nameof(value))
                : value.Trim();

            return normalized.Length <= MaxNameLength
                ? normalized
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: $"Care facility names cannot exceed {MaxNameLength} characters.");
        }

        private static int EnsureCapacity(int value)
        {
            return value > 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: "Daily patient capacity must be positive.");
        }

        private static long EnsureRevision(long value)
        {
            return value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: "Care facility source revisions cannot be negative.");
        }

        private static DateTimeOffset EnsureUtc(DateTimeOffset value)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : throw new ArgumentException(
                    message: "Care facility synchronization timestamps must be expressed in UTC.",
                    paramName: nameof(value));
        }
    }
}
