using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Simulation;

namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Systems
{
    /// <summary>
    ///     Aggregate root for citywide material reserves and supply resilience.
    /// </summary>
    public sealed class CityStockpileState : AggregateRoot<SimulationHostId>
    {
        private CityStockpileState(
            SimulationHostId simulationHostId,
            CityResourceStockLineState fuel,
            CityResourceStockLineState food,
            CityResourceStockLineState medicine,
            CityResourceStockLineState spareParts,
            CityResourceStockLineState filters,
            CityResourceStockLineState emergencyWater,
            CitySystemsResourceDemandState systemsDemand,
            decimal supplyStressIndex,
            bool emergencyRationingEnabled,
            DateTimeOffset lastEvaluatedAtUtc)
            : base(simulationHostId)
        {
            Fuel = fuel;
            Food = food;
            Medicine = medicine;
            SpareParts = spareParts;
            Filters = filters;
            EmergencyWater = emergencyWater;
            SystemsDemand = systemsDemand;
            SupplyStressIndex = EnsureIndex(
                value: supplyStressIndex,
                propertyName: nameof(supplyStressIndex));
            EmergencyRationingEnabled = emergencyRationingEnabled;
            LastEvaluatedAtUtc = EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
        }

        private CityStockpileState()
            : base(default(SimulationHostId))
        {
            Fuel = null!;
            Food = null!;
            Medicine = null!;
            SpareParts = null!;
            Filters = null!;
            EmergencyWater = null!;
            SystemsDemand = null!;
        }

        public SimulationHostId SimulationHostId => Id;
        public CityResourceStockLineState Fuel { get; private set; }
        public CityResourceStockLineState Food { get; private set; }
        public CityResourceStockLineState Medicine { get; private set; }
        public CityResourceStockLineState SpareParts { get; private set; }
        public CityResourceStockLineState Filters { get; private set; }
        public CityResourceStockLineState EmergencyWater { get; private set; }
        public CitySystemsResourceDemandState SystemsDemand { get; private set; }
        public decimal SupplyStressIndex { get; private set; }
        public bool EmergencyRationingEnabled { get; private set; }
        public DateTimeOffset LastEvaluatedAtUtc { get; private set; }

        public static CityStockpileState Create(
            SimulationHostId simulationHostId,
            CityStockpileSnapshot seed)
        {
            GuardHelper.AgainstNull(
                value: seed,
                propertyName: nameof(seed));

            return new CityStockpileState(
                simulationHostId: simulationHostId,
                fuel: CityResourceStockLineState.Create(seed.Fuel),
                food: CityResourceStockLineState.Create(seed.Food),
                medicine: CityResourceStockLineState.Create(seed.Medicine),
                spareParts: CityResourceStockLineState.Create(seed.SpareParts),
                filters: CityResourceStockLineState.Create(seed.Filters),
                emergencyWater: CityResourceStockLineState.Create(seed.EmergencyWater),
                systemsDemand: CitySystemsResourceDemandState.Create(seed.SystemsDemand),
                supplyStressIndex: seed.SupplyStressIndex,
                emergencyRationingEnabled: seed.EmergencyRationingEnabled,
                lastEvaluatedAtUtc: seed.EvaluatedAtUtc);
        }

        public void ApplySystemsDemand(CitySystemsResourceDemandSnapshot snapshot)
        {
            GuardHelper.AgainstNull(
                value: snapshot,
                propertyName: nameof(snapshot));

            SystemsDemand.ApplySnapshot(snapshot);
        }

        public void ApplySnapshot(CityStockpileSnapshot snapshot)
        {
            GuardHelper.AgainstNull(
                value: snapshot,
                propertyName: nameof(snapshot));

            if (snapshot.EvaluatedAtUtc < LastEvaluatedAtUtc)
                throw new InvalidOperationException("Stockpile snapshots cannot move backwards in time.");

            Fuel.ApplySnapshot(snapshot.Fuel);
            Food.ApplySnapshot(snapshot.Food);
            Medicine.ApplySnapshot(snapshot.Medicine);
            SpareParts.ApplySnapshot(snapshot.SpareParts);
            Filters.ApplySnapshot(snapshot.Filters);
            EmergencyWater.ApplySnapshot(snapshot.EmergencyWater);
            SystemsDemand.ApplySnapshot(snapshot.SystemsDemand);
            SupplyStressIndex = EnsureIndex(
                value: snapshot.SupplyStressIndex,
                propertyName: nameof(snapshot.SupplyStressIndex));
            EmergencyRationingEnabled = snapshot.EmergencyRationingEnabled;
            LastEvaluatedAtUtc = EnsureUtc(
                value: snapshot.EvaluatedAtUtc,
                paramName: nameof(snapshot.EvaluatedAtUtc));
        }

        public CityStockpileSnapshot ToSnapshot()
        {
            return new CityStockpileSnapshot(
                Fuel: Fuel.ToSnapshot(),
                Food: Food.ToSnapshot(),
                Medicine: Medicine.ToSnapshot(),
                SpareParts: SpareParts.ToSnapshot(),
                Filters: Filters.ToSnapshot(),
                EmergencyWater: EmergencyWater.ToSnapshot(),
                SystemsDemand: SystemsDemand.ToSnapshot(),
                SupplyStressIndex: SupplyStressIndex,
                EmergencyRationingEnabled: EmergencyRationingEnabled,
                EvaluatedAtUtc: LastEvaluatedAtUtc);
        }

        private static decimal EnsureIndex(
            decimal value,
            string propertyName)
        {
            return decimal.Round(
                d: GuardHelper.AgainstOutOfRange(
                    value: value,
                    min: 0m,
                    max: 1m,
                    propertyName: propertyName),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static DateTimeOffset EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : throw new ArgumentException(
                    message: "Timestamps must be expressed in UTC.",
                    paramName: paramName);
        }
    }
}
