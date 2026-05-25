using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CitySanitationInfrastructureState
    {
        private CitySanitationInfrastructureState() { }

        private CitySanitationInfrastructureState(
            decimal treatmentStabilityIndex,
            decimal networkIntegrityIndex,
            decimal overflowControlIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            TreatmentStabilityIndex = treatmentStabilityIndex;
            NetworkIntegrityIndex = networkIntegrityIndex;
            OverflowControlIndex = overflowControlIndex;
            CrewReadinessIndex = crewReadinessIndex;
            IncidentPressureIndex = incidentPressureIndex;
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal TreatmentStabilityIndex { get; private set; }
        public decimal NetworkIntegrityIndex { get; private set; }
        public decimal OverflowControlIndex { get; private set; }
        public decimal CrewReadinessIndex { get; private set; }
        public decimal IncidentPressureIndex { get; private set; }
        public bool EmergencyModeEnabled { get; private set; }

        public static CitySanitationInfrastructureState Create(CitySanitationInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CitySanitationInfrastructureState(
                treatmentStabilityIndex: snapshot.TreatmentStabilityIndex,
                networkIntegrityIndex: snapshot.NetworkIntegrityIndex,
                overflowControlIndex: snapshot.OverflowControlIndex,
                crewReadinessIndex: snapshot.CrewReadinessIndex,
                incidentPressureIndex: snapshot.IncidentPressureIndex,
                emergencyModeEnabled: snapshot.EmergencyModeEnabled);
        }

        public void ApplySnapshot(CitySanitationInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            TreatmentStabilityIndex = snapshot.TreatmentStabilityIndex;
            NetworkIntegrityIndex = snapshot.NetworkIntegrityIndex;
            OverflowControlIndex = snapshot.OverflowControlIndex;
            CrewReadinessIndex = snapshot.CrewReadinessIndex;
            IncidentPressureIndex = snapshot.IncidentPressureIndex;
            EmergencyModeEnabled = snapshot.EmergencyModeEnabled;
        }

        public void SetEmergencyMode(bool enabled)
        {
            EmergencyModeEnabled = enabled;
        }

        public void DispatchMaintenance(
            SanitationMaintenanceFocus focus,
            SanitationMaintenanceIntensity intensity)
        {
            decimal intensityFactor = intensity switch
            {
                SanitationMaintenanceIntensity.Light => 0.45m,
                SanitationMaintenanceIntensity.Standard => 0.72m,
                SanitationMaintenanceIntensity.Heavy => 0.95m,
                _ => throw new ArgumentOutOfRangeException(nameof(intensity))
            };

            decimal treatmentBoost = focus switch
            {
                SanitationMaintenanceFocus.Balanced => 0.0700m,
                SanitationMaintenanceFocus.TreatmentStabilization => 0.1400m,
                SanitationMaintenanceFocus.SewerRepairs => 0.0400m,
                SanitationMaintenanceFocus.OverflowControl => 0.0400m,
                SanitationMaintenanceFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal networkBoost = focus switch
            {
                SanitationMaintenanceFocus.Balanced => 0.0600m,
                SanitationMaintenanceFocus.TreatmentStabilization => 0.0300m,
                SanitationMaintenanceFocus.SewerRepairs => 0.1350m,
                SanitationMaintenanceFocus.OverflowControl => 0.0400m,
                SanitationMaintenanceFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal overflowBoost = focus switch
            {
                SanitationMaintenanceFocus.Balanced => 0.0550m,
                SanitationMaintenanceFocus.TreatmentStabilization => 0.0350m,
                SanitationMaintenanceFocus.SewerRepairs => 0.0500m,
                SanitationMaintenanceFocus.OverflowControl => 0.1300m,
                SanitationMaintenanceFocus.CrewRecovery => 0.0250m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal incidentRelief = focus switch
            {
                SanitationMaintenanceFocus.Balanced => 0.0600m,
                SanitationMaintenanceFocus.TreatmentStabilization => 0.0700m,
                SanitationMaintenanceFocus.SewerRepairs => 0.0800m,
                SanitationMaintenanceFocus.OverflowControl => 0.0800m,
                SanitationMaintenanceFocus.CrewRecovery => 0.0400m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal crewDelta = focus switch
            {
                SanitationMaintenanceFocus.Balanced => -0.0400m,
                SanitationMaintenanceFocus.TreatmentStabilization => -0.0450m,
                SanitationMaintenanceFocus.SewerRepairs => -0.0550m,
                SanitationMaintenanceFocus.OverflowControl => -0.0500m,
                SanitationMaintenanceFocus.CrewRecovery => 0.0650m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };

            TreatmentStabilityIndex = ClampAndRound(TreatmentStabilityIndex + (treatmentBoost * intensityFactor));
            NetworkIntegrityIndex = ClampAndRound(NetworkIntegrityIndex + (networkBoost * intensityFactor));
            OverflowControlIndex = ClampAndRound(OverflowControlIndex + (overflowBoost * intensityFactor));
            IncidentPressureIndex = ClampAndRound(IncidentPressureIndex - (incidentRelief * intensityFactor));
            CrewReadinessIndex = ClampAndRound(
                CrewReadinessIndex +
                (crewDelta * intensityFactor) +
                (EmergencyModeEnabled
                    ? -0.0200m
                    : 0.0200m));
        }

        public CitySanitationInfrastructureSnapshot ToSnapshot()
        {
            return new CitySanitationInfrastructureSnapshot(
                treatmentStabilityIndex: TreatmentStabilityIndex,
                networkIntegrityIndex: NetworkIntegrityIndex,
                overflowControlIndex: OverflowControlIndex,
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
