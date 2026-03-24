using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityHeatingInfrastructureState
    {
        private CityHeatingInfrastructureState() { }

        private CityHeatingInfrastructureState(
            decimal plantCapacityIndex,
            decimal networkIntegrityIndex,
            decimal controlReadinessIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            PlantCapacityIndex = plantCapacityIndex;
            NetworkIntegrityIndex = networkIntegrityIndex;
            ControlReadinessIndex = controlReadinessIndex;
            CrewReadinessIndex = crewReadinessIndex;
            IncidentPressureIndex = incidentPressureIndex;
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal PlantCapacityIndex { get; private set; }
        public decimal NetworkIntegrityIndex { get; private set; }
        public decimal ControlReadinessIndex { get; private set; }
        public decimal CrewReadinessIndex { get; private set; }
        public decimal IncidentPressureIndex { get; private set; }
        public bool EmergencyModeEnabled { get; private set; }

        public static CityHeatingInfrastructureState Create(
            CityHeatingInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CityHeatingInfrastructureState(
                plantCapacityIndex: snapshot.PlantCapacityIndex,
                networkIntegrityIndex: snapshot.NetworkIntegrityIndex,
                controlReadinessIndex: snapshot.ControlReadinessIndex,
                crewReadinessIndex: snapshot.CrewReadinessIndex,
                incidentPressureIndex: snapshot.IncidentPressureIndex,
                emergencyModeEnabled: snapshot.EmergencyModeEnabled);
        }

        public void ApplySnapshot(CityHeatingInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            PlantCapacityIndex = snapshot.PlantCapacityIndex;
            NetworkIntegrityIndex = snapshot.NetworkIntegrityIndex;
            ControlReadinessIndex = snapshot.ControlReadinessIndex;
            CrewReadinessIndex = snapshot.CrewReadinessIndex;
            IncidentPressureIndex = snapshot.IncidentPressureIndex;
            EmergencyModeEnabled = snapshot.EmergencyModeEnabled;
        }

        public void SetEmergencyMode(bool enabled)
        {
            EmergencyModeEnabled = enabled;
        }

        public void DispatchMaintenance(
            HeatingMaintenanceFocus focus,
            HeatingMaintenanceIntensity intensity)
        {
            decimal intensityFactor = intensity switch
            {
                HeatingMaintenanceIntensity.Light => 0.45m,
                HeatingMaintenanceIntensity.Standard => 0.72m,
                HeatingMaintenanceIntensity.Heavy => 0.95m,
                _ => throw new ArgumentOutOfRangeException(nameof(intensity))
            };

            decimal plantBoost = focus switch
            {
                HeatingMaintenanceFocus.Balanced => 0.0700m,
                HeatingMaintenanceFocus.PlantRepairs => 0.1400m,
                HeatingMaintenanceFocus.NetworkStabilization => 0.0400m,
                HeatingMaintenanceFocus.ControlCalibration => 0.0400m,
                HeatingMaintenanceFocus.CrewRecovery => 0.0250m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal networkBoost = focus switch
            {
                HeatingMaintenanceFocus.Balanced => 0.0600m,
                HeatingMaintenanceFocus.PlantRepairs => 0.0350m,
                HeatingMaintenanceFocus.NetworkStabilization => 0.1300m,
                HeatingMaintenanceFocus.ControlCalibration => 0.0350m,
                HeatingMaintenanceFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal controlBoost = focus switch
            {
                HeatingMaintenanceFocus.Balanced => 0.0550m,
                HeatingMaintenanceFocus.PlantRepairs => 0.0300m,
                HeatingMaintenanceFocus.NetworkStabilization => 0.0450m,
                HeatingMaintenanceFocus.ControlCalibration => 0.1300m,
                HeatingMaintenanceFocus.CrewRecovery => 0.0250m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal incidentRelief = focus switch
            {
                HeatingMaintenanceFocus.Balanced => 0.0600m,
                HeatingMaintenanceFocus.PlantRepairs => 0.0700m,
                HeatingMaintenanceFocus.NetworkStabilization => 0.0800m,
                HeatingMaintenanceFocus.ControlCalibration => 0.0600m,
                HeatingMaintenanceFocus.CrewRecovery => 0.0400m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal crewDelta = focus switch
            {
                HeatingMaintenanceFocus.Balanced => -0.0400m,
                HeatingMaintenanceFocus.PlantRepairs => -0.0500m,
                HeatingMaintenanceFocus.NetworkStabilization => -0.0550m,
                HeatingMaintenanceFocus.ControlCalibration => -0.0400m,
                HeatingMaintenanceFocus.CrewRecovery => 0.0650m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };

            PlantCapacityIndex = ClampAndRound(PlantCapacityIndex + (plantBoost * intensityFactor));
            NetworkIntegrityIndex = ClampAndRound(NetworkIntegrityIndex + (networkBoost * intensityFactor));
            ControlReadinessIndex = ClampAndRound(ControlReadinessIndex + (controlBoost * intensityFactor));
            IncidentPressureIndex = ClampAndRound(IncidentPressureIndex - (incidentRelief * intensityFactor));
            CrewReadinessIndex = ClampAndRound(
                CrewReadinessIndex + (crewDelta * intensityFactor) + (EmergencyModeEnabled ? -0.0200m : 0.0200m));
        }

        public CityHeatingInfrastructureSnapshot ToSnapshot()
        {
            return new CityHeatingInfrastructureSnapshot(
                plantCapacityIndex: PlantCapacityIndex,
                networkIntegrityIndex: NetworkIntegrityIndex,
                controlReadinessIndex: ControlReadinessIndex,
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
