using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityDrainageInfrastructureState
    {
        private CityDrainageInfrastructureState() { }

        private CityDrainageInfrastructureState(
            decimal pumpCapacityIndex,
            decimal networkIntegrityIndex,
            decimal blockageIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            PumpCapacityIndex = pumpCapacityIndex;
            NetworkIntegrityIndex = networkIntegrityIndex;
            BlockageIndex = blockageIndex;
            CrewReadinessIndex = crewReadinessIndex;
            IncidentPressureIndex = incidentPressureIndex;
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal PumpCapacityIndex { get; private set; }
        public decimal NetworkIntegrityIndex { get; private set; }
        public decimal BlockageIndex { get; private set; }
        public decimal CrewReadinessIndex { get; private set; }
        public decimal IncidentPressureIndex { get; private set; }
        public bool EmergencyModeEnabled { get; private set; }

        public static CityDrainageInfrastructureState Create(CityDrainageInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CityDrainageInfrastructureState(
                pumpCapacityIndex: snapshot.PumpCapacityIndex,
                networkIntegrityIndex: snapshot.NetworkIntegrityIndex,
                blockageIndex: snapshot.BlockageIndex,
                crewReadinessIndex: snapshot.CrewReadinessIndex,
                incidentPressureIndex: snapshot.IncidentPressureIndex,
                emergencyModeEnabled: snapshot.EmergencyModeEnabled);
        }

        public void ApplySnapshot(CityDrainageInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            PumpCapacityIndex = snapshot.PumpCapacityIndex;
            NetworkIntegrityIndex = snapshot.NetworkIntegrityIndex;
            BlockageIndex = snapshot.BlockageIndex;
            CrewReadinessIndex = snapshot.CrewReadinessIndex;
            IncidentPressureIndex = snapshot.IncidentPressureIndex;
            EmergencyModeEnabled = snapshot.EmergencyModeEnabled;
        }

        public void SetEmergencyMode(bool enabled)
        {
            EmergencyModeEnabled = enabled;
        }

        public void DispatchMaintenance(
            DrainageMaintenanceFocus focus,
            DrainageMaintenanceIntensity intensity)
        {
            decimal intensityFactor = intensity switch
            {
                DrainageMaintenanceIntensity.Light => 0.45m,
                DrainageMaintenanceIntensity.Standard => 0.72m,
                DrainageMaintenanceIntensity.Heavy => 0.95m,
                _ => throw new ArgumentOutOfRangeException(nameof(intensity))
            };

            decimal pumpBoost = focus switch
            {
                DrainageMaintenanceFocus.Balanced => 0.0600m,
                DrainageMaintenanceFocus.BlockageClearance => 0.0350m,
                DrainageMaintenanceFocus.PumpRepairs => 0.1100m,
                DrainageMaintenanceFocus.NetworkStabilization => 0.0400m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal integrityBoost = focus switch
            {
                DrainageMaintenanceFocus.Balanced => 0.0550m,
                DrainageMaintenanceFocus.BlockageClearance => 0.0250m,
                DrainageMaintenanceFocus.PumpRepairs => 0.0300m,
                DrainageMaintenanceFocus.NetworkStabilization => 0.1100m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal blockageReduction = focus switch
            {
                DrainageMaintenanceFocus.Balanced => 0.0750m,
                DrainageMaintenanceFocus.BlockageClearance => 0.1400m,
                DrainageMaintenanceFocus.PumpRepairs => 0.0450m,
                DrainageMaintenanceFocus.NetworkStabilization => 0.0550m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal incidentRelief = focus switch
            {
                DrainageMaintenanceFocus.Balanced => 0.0600m,
                DrainageMaintenanceFocus.BlockageClearance => 0.0700m,
                DrainageMaintenanceFocus.PumpRepairs => 0.0650m,
                DrainageMaintenanceFocus.NetworkStabilization => 0.0800m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };

            PumpCapacityIndex = ClampAndRound(PumpCapacityIndex + (pumpBoost * intensityFactor));
            NetworkIntegrityIndex = ClampAndRound(NetworkIntegrityIndex + (integrityBoost * intensityFactor));
            BlockageIndex = ClampAndRound(BlockageIndex - (blockageReduction * intensityFactor));
            IncidentPressureIndex = ClampAndRound(IncidentPressureIndex - (incidentRelief * intensityFactor));
            CrewReadinessIndex = ClampAndRound(
                CrewReadinessIndex - (0.0550m * intensityFactor) + (EmergencyModeEnabled ? -0.0200m : 0.0150m));
        }

        public CityDrainageInfrastructureSnapshot ToSnapshot()
        {
            return new CityDrainageInfrastructureSnapshot(
                pumpCapacityIndex: PumpCapacityIndex,
                networkIntegrityIndex: NetworkIntegrityIndex,
                blockageIndex: BlockageIndex,
                crewReadinessIndex: CrewReadinessIndex,
                incidentPressureIndex: IncidentPressureIndex,
                emergencyModeEnabled: EmergencyModeEnabled);
        }

        private static decimal ClampAndRound(decimal value)
        {
            return decimal.Round(
                d: Math.Min(
                    val1: 1m,
                    val2: Math.Max(
                        val1: 0m,
                        val2: value)),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
