using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CitySystemsResourceDemandState
    {
        private CitySystemsResourceDemandState() { }

        private CitySystemsResourceDemandState(
            decimal fuelDemandPressureIndex,
            decimal sparePartsDemandPressureIndex,
            decimal filtersDemandPressureIndex,
            decimal emergencyWaterDemandPressureIndex,
            decimal overallDemandPressureIndex,
            DateTimeOffset effectiveAtUtc)
        {
            FuelDemandPressureIndex = fuelDemandPressureIndex;
            SparePartsDemandPressureIndex = sparePartsDemandPressureIndex;
            FiltersDemandPressureIndex = filtersDemandPressureIndex;
            EmergencyWaterDemandPressureIndex = emergencyWaterDemandPressureIndex;
            OverallDemandPressureIndex = overallDemandPressureIndex;
            EffectiveAtUtc = effectiveAtUtc;
        }

        public decimal FuelDemandPressureIndex { get; private set; }
        public decimal SparePartsDemandPressureIndex { get; private set; }
        public decimal FiltersDemandPressureIndex { get; private set; }
        public decimal EmergencyWaterDemandPressureIndex { get; private set; }
        public decimal OverallDemandPressureIndex { get; private set; }
        public DateTimeOffset EffectiveAtUtc { get; private set; }

        public static CitySystemsResourceDemandState Create(CitySystemsResourceDemandSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CitySystemsResourceDemandState(
                fuelDemandPressureIndex: snapshot.FuelDemandPressureIndex,
                sparePartsDemandPressureIndex: snapshot.SparePartsDemandPressureIndex,
                filtersDemandPressureIndex: snapshot.FiltersDemandPressureIndex,
                emergencyWaterDemandPressureIndex: snapshot.EmergencyWaterDemandPressureIndex,
                overallDemandPressureIndex: snapshot.OverallDemandPressureIndex,
                effectiveAtUtc: snapshot.EffectiveAtUtc);
        }

        public void ApplySnapshot(CitySystemsResourceDemandSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            FuelDemandPressureIndex = snapshot.FuelDemandPressureIndex;
            SparePartsDemandPressureIndex = snapshot.SparePartsDemandPressureIndex;
            FiltersDemandPressureIndex = snapshot.FiltersDemandPressureIndex;
            EmergencyWaterDemandPressureIndex = snapshot.EmergencyWaterDemandPressureIndex;
            OverallDemandPressureIndex = snapshot.OverallDemandPressureIndex;
            EffectiveAtUtc = snapshot.EffectiveAtUtc;
        }

        public CitySystemsResourceDemandSnapshot ToSnapshot()
        {
            return new CitySystemsResourceDemandSnapshot(
                FuelDemandPressureIndex: FuelDemandPressureIndex,
                SparePartsDemandPressureIndex: SparePartsDemandPressureIndex,
                FiltersDemandPressureIndex: FiltersDemandPressureIndex,
                EmergencyWaterDemandPressureIndex: EmergencyWaterDemandPressureIndex,
                OverallDemandPressureIndex: OverallDemandPressureIndex,
                EffectiveAtUtc: EffectiveAtUtc);
        }
    }
}
