using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Domain.Operations;

public sealed class CareMedicineSupplyState : AggregateRoot<SimulationHostId>
{
    private CareMedicineSupplyState(
        SimulationHostId simulationHostId,
        CareAvailabilityIndex stockLevel,
        CareAvailabilityIndex shortageRisk,
        long sourceRevision,
        DateTimeOffset observedAtUtc)
        : base(simulationHostId)
    {
        StockLevel = stockLevel;
        ShortageRisk = shortageRisk;
        LastSourceRevision = EnsureRevision(sourceRevision);
        LastObservedAtUtc = EnsureUtc(observedAtUtc);
    }

    private CareMedicineSupplyState()
        : base(default(SimulationHostId))
    {
    }

    public SimulationHostId SimulationHostId => Id;
    public CareAvailabilityIndex StockLevel { get; private set; }
    public CareAvailabilityIndex ShortageRisk { get; private set; }
    public long LastSourceRevision { get; private set; }
    public DateTimeOffset LastObservedAtUtc { get; private set; }

    public static CareMedicineSupplyState Register(
        SimulationHostId simulationHostId,
        CareAvailabilityIndex stockLevel,
        CareAvailabilityIndex shortageRisk,
        long sourceRevision,
        DateTimeOffset observedAtUtc)
    {
        return new CareMedicineSupplyState(
            simulationHostId,
            stockLevel,
            shortageRisk,
            sourceRevision,
            observedAtUtc);
    }

    public bool TrySynchronize(
        CareAvailabilityIndex stockLevel,
        CareAvailabilityIndex shortageRisk,
        long sourceRevision,
        DateTimeOffset observedAtUtc)
    {
        long normalizedRevision = EnsureRevision(sourceRevision);
        DateTimeOffset normalizedTimestamp = EnsureUtc(observedAtUtc);
        if (normalizedRevision < LastSourceRevision)
            return false;
        if (normalizedRevision == LastSourceRevision)
        {
            if (stockLevel != StockLevel
                || shortageRisk != ShortageRisk
                || normalizedTimestamp != LastObservedAtUtc)
                throw new InvalidOperationException(
                    "A medicine supply revision cannot identify conflicting values.");

            return false;
        }

        StockLevel = stockLevel;
        ShortageRisk = shortageRisk;
        LastSourceRevision = normalizedRevision;
        LastObservedAtUtc = normalizedTimestamp;
        return true;
    }

    private static long EnsureRevision(long value)
    {
        return value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(
                paramName: nameof(value),
                message: "Medicine supply revisions cannot be negative.");
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        return value.Offset == TimeSpan.Zero
            ? value
            : throw new ArgumentException(
                message: "Medicine supply observation timestamps must be expressed in UTC.",
                paramName: nameof(value));
    }
}
