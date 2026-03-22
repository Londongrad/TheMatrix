using Matrix.BuildingBlocks.Domain.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.SimulationSystems.Domain.Simulation;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    /// <summary>
    ///     Aggregate root for physical city conditions driven by weather pressure and system response.
    /// </summary>
    public sealed class CityEnvironmentalConditionState : AggregateRoot<SimulationHostId>
    {
        private CityEnvironmentalConditionState(
            SimulationHostId simulationHostId,
            CitySystemState drainage,
            CitySystemState snowRemoval,
            CitySystemState roadAccess,
            FloodingIndex floodingIndex,
            SnowAccumulationIndex snowAccumulationIndex,
            RoadAccessibilityIndex roadAccessibilityIndex,
            DateTimeOffset lastEvaluatedAtUtc)
            : base(simulationHostId)
        {
            Drainage = drainage;
            SnowRemoval = snowRemoval;
            RoadAccess = roadAccess;
            FloodingIndex = floodingIndex;
            SnowAccumulationIndex = snowAccumulationIndex;
            RoadAccessibilityIndex = roadAccessibilityIndex;
            LastEvaluatedAtUtc = EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
        }

        private CityEnvironmentalConditionState()
            : base(default(SimulationHostId))
        {
            Drainage = null!;
            SnowRemoval = null!;
            RoadAccess = null!;
        }

        public SimulationHostId SimulationHostId => Id;
        public CitySystemState Drainage { get; private set; }
        public CitySystemState SnowRemoval { get; private set; }
        public CitySystemState RoadAccess { get; private set; }
        public FloodingIndex FloodingIndex { get; private set; }
        public SnowAccumulationIndex SnowAccumulationIndex { get; private set; }
        public RoadAccessibilityIndex RoadAccessibilityIndex { get; private set; }
        public DateTimeOffset LastEvaluatedAtUtc { get; private set; }

        public static CityEnvironmentalConditionState Create(
            SimulationHostId simulationHostId,
            CityEnvironmentalConditionSnapshot seed)
        {
            ArgumentNullException.ThrowIfNull(seed);

            return new CityEnvironmentalConditionState(
                simulationHostId: simulationHostId,
                drainage: CitySystemState.Create(seed.Drainage),
                snowRemoval: CitySystemState.Create(seed.SnowRemoval),
                roadAccess: CitySystemState.Create(seed.RoadAccess),
                floodingIndex: seed.FloodingIndex,
                snowAccumulationIndex: seed.SnowAccumulationIndex,
                roadAccessibilityIndex: seed.RoadAccessibilityIndex,
                lastEvaluatedAtUtc: seed.EvaluatedAtUtc);
        }

        public void ApplySnapshot(CityEnvironmentalConditionSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            if (snapshot.EvaluatedAtUtc < LastEvaluatedAtUtc)
                throw new ArgumentException(
                    message: "Environmental condition snapshots cannot move backwards in time.",
                    paramName: nameof(snapshot));

            Drainage.ApplySnapshot(snapshot.Drainage);
            SnowRemoval.ApplySnapshot(snapshot.SnowRemoval);
            RoadAccess.ApplySnapshot(snapshot.RoadAccess);
            FloodingIndex = snapshot.FloodingIndex;
            SnowAccumulationIndex = snapshot.SnowAccumulationIndex;
            RoadAccessibilityIndex = snapshot.RoadAccessibilityIndex;
            LastEvaluatedAtUtc = snapshot.EvaluatedAtUtc;
        }

        public CityEnvironmentalConditionSnapshot ToSnapshot()
        {
            return new CityEnvironmentalConditionSnapshot(
                drainage: Drainage.ToSnapshot(),
                snowRemoval: SnowRemoval.ToSnapshot(),
                roadAccess: RoadAccess.ToSnapshot(),
                floodingIndex: FloodingIndex,
                snowAccumulationIndex: SnowAccumulationIndex,
                roadAccessibilityIndex: RoadAccessibilityIndex,
                evaluatedAtUtc: LastEvaluatedAtUtc);
        }

        private static DateTimeOffset EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : throw new ArgumentException(
                    message: "Timestamp must be UTC.",
                    paramName: paramName);
        }
    }
}
