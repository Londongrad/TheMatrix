using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityPowerDistributionInfrastructureState
    {
        private CityPowerDistributionInfrastructureState() { }

        private CityPowerDistributionInfrastructureState(
            decimal substationCapacityIndex,
            decimal gridIntegrityIndex,
            decimal switchingReadinessIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            SubstationCapacityIndex = substationCapacityIndex;
            GridIntegrityIndex = gridIntegrityIndex;
            SwitchingReadinessIndex = switchingReadinessIndex;
            CrewReadinessIndex = crewReadinessIndex;
            IncidentPressureIndex = incidentPressureIndex;
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal SubstationCapacityIndex { get; private set; }
        public decimal GridIntegrityIndex { get; private set; }
        public decimal SwitchingReadinessIndex { get; private set; }
        public decimal CrewReadinessIndex { get; private set; }
        public decimal IncidentPressureIndex { get; private set; }
        public bool EmergencyModeEnabled { get; private set; }

        public static CityPowerDistributionInfrastructureState Create(
            CityPowerDistributionInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CityPowerDistributionInfrastructureState(
                substationCapacityIndex: snapshot.SubstationCapacityIndex,
                gridIntegrityIndex: snapshot.GridIntegrityIndex,
                switchingReadinessIndex: snapshot.SwitchingReadinessIndex,
                crewReadinessIndex: snapshot.CrewReadinessIndex,
                incidentPressureIndex: snapshot.IncidentPressureIndex,
                emergencyModeEnabled: snapshot.EmergencyModeEnabled);
        }

        public void ApplySnapshot(CityPowerDistributionInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            SubstationCapacityIndex = snapshot.SubstationCapacityIndex;
            GridIntegrityIndex = snapshot.GridIntegrityIndex;
            SwitchingReadinessIndex = snapshot.SwitchingReadinessIndex;
            CrewReadinessIndex = snapshot.CrewReadinessIndex;
            IncidentPressureIndex = snapshot.IncidentPressureIndex;
            EmergencyModeEnabled = snapshot.EmergencyModeEnabled;
        }

        public void SetEmergencyMode(bool enabled)
        {
            EmergencyModeEnabled = enabled;
        }

        public void DispatchMaintenance(
            PowerDistributionMaintenanceFocus focus,
            PowerDistributionMaintenanceIntensity intensity)
        {
            decimal intensityFactor = intensity switch
            {
                PowerDistributionMaintenanceIntensity.Light => 0.45m,
                PowerDistributionMaintenanceIntensity.Standard => 0.72m,
                PowerDistributionMaintenanceIntensity.Heavy => 0.95m,
                _ => throw new ArgumentOutOfRangeException(nameof(intensity))
            };

            decimal substationBoost = focus switch
            {
                PowerDistributionMaintenanceFocus.Balanced => 0.0700m,
                PowerDistributionMaintenanceFocus.SubstationRepairs => 0.1450m,
                PowerDistributionMaintenanceFocus.GridStabilization => 0.0400m,
                PowerDistributionMaintenanceFocus.SwitchingRecovery => 0.0450m,
                PowerDistributionMaintenanceFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal gridBoost = focus switch
            {
                PowerDistributionMaintenanceFocus.Balanced => 0.0600m,
                PowerDistributionMaintenanceFocus.SubstationRepairs => 0.0300m,
                PowerDistributionMaintenanceFocus.GridStabilization => 0.1350m,
                PowerDistributionMaintenanceFocus.SwitchingRecovery => 0.0450m,
                PowerDistributionMaintenanceFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal switchingBoost = focus switch
            {
                PowerDistributionMaintenanceFocus.Balanced => 0.0550m,
                PowerDistributionMaintenanceFocus.SubstationRepairs => 0.0400m,
                PowerDistributionMaintenanceFocus.GridStabilization => 0.0450m,
                PowerDistributionMaintenanceFocus.SwitchingRecovery => 0.1300m,
                PowerDistributionMaintenanceFocus.CrewRecovery => 0.0250m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal incidentRelief = focus switch
            {
                PowerDistributionMaintenanceFocus.Balanced => 0.0600m,
                PowerDistributionMaintenanceFocus.SubstationRepairs => 0.0800m,
                PowerDistributionMaintenanceFocus.GridStabilization => 0.0850m,
                PowerDistributionMaintenanceFocus.SwitchingRecovery => 0.0750m,
                PowerDistributionMaintenanceFocus.CrewRecovery => 0.0400m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal crewDelta = focus switch
            {
                PowerDistributionMaintenanceFocus.Balanced => -0.0400m,
                PowerDistributionMaintenanceFocus.SubstationRepairs => -0.0500m,
                PowerDistributionMaintenanceFocus.GridStabilization => -0.0550m,
                PowerDistributionMaintenanceFocus.SwitchingRecovery => -0.0500m,
                PowerDistributionMaintenanceFocus.CrewRecovery => 0.0650m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };

            SubstationCapacityIndex = ClampAndRound(SubstationCapacityIndex + (substationBoost * intensityFactor));
            GridIntegrityIndex = ClampAndRound(GridIntegrityIndex + (gridBoost * intensityFactor));
            SwitchingReadinessIndex = ClampAndRound(SwitchingReadinessIndex + (switchingBoost * intensityFactor));
            IncidentPressureIndex = ClampAndRound(IncidentPressureIndex - (incidentRelief * intensityFactor));
            CrewReadinessIndex = ClampAndRound(
                CrewReadinessIndex +
                (crewDelta * intensityFactor) +
                (EmergencyModeEnabled
                    ? -0.0200m
                    : 0.0200m));
        }

        public CityPowerDistributionInfrastructureSnapshot ToSnapshot()
        {
            return new CityPowerDistributionInfrastructureSnapshot(
                substationCapacityIndex: SubstationCapacityIndex,
                gridIntegrityIndex: GridIntegrityIndex,
                switchingReadinessIndex: SwitchingReadinessIndex,
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
