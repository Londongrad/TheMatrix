using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Domain.Operations;

public sealed class CareServiceQualityState : AggregateRoot<SimulationHostId>
{
    private CareServiceQualityState(
        SimulationHostId simulationHostId,
        CareQualityMultiplier qualityMultiplier,
        DateTimeOffset observedAtUtc)
        : base(simulationHostId)
    {
        QualityMultiplier = qualityMultiplier;
        LastObservedAtUtc = EnsureUtc(observedAtUtc);
    }

    private CareServiceQualityState()
        : base(default(SimulationHostId))
    {
    }

    public SimulationHostId SimulationHostId => Id;
    public CareQualityMultiplier QualityMultiplier { get; private set; }
    public DateTimeOffset LastObservedAtUtc { get; private set; }

    public static CareServiceQualityState Register(
        SimulationHostId simulationHostId,
        CareQualityMultiplier qualityMultiplier,
        DateTimeOffset observedAtUtc)
    {
        return new CareServiceQualityState(
            simulationHostId,
            qualityMultiplier,
            observedAtUtc);
    }

    public bool TrySynchronize(
        CareQualityMultiplier qualityMultiplier,
        DateTimeOffset observedAtUtc)
    {
        DateTimeOffset normalizedTimestamp = EnsureUtc(observedAtUtc);
        if (normalizedTimestamp < LastObservedAtUtc)
            return false;
        if (normalizedTimestamp == LastObservedAtUtc)
        {
            if (qualityMultiplier != QualityMultiplier)
                throw new InvalidOperationException(
                    "A care quality observation timestamp cannot identify conflicting values.");

            return false;
        }

        QualityMultiplier = qualityMultiplier;
        LastObservedAtUtc = normalizedTimestamp;
        return true;
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        return value.Offset == TimeSpan.Zero
            ? value
            : throw new ArgumentException(
                message: "Care quality observation timestamps must be expressed in UTC.",
                paramName: nameof(value));
    }
}
