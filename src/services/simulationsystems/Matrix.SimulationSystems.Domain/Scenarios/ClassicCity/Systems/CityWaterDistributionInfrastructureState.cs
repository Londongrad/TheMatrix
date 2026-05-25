using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityWaterDistributionInfrastructureState
    {
        private CityWaterDistributionInfrastructureState() { }

        private CityWaterDistributionInfrastructureState(
            decimal treatmentCapacityIndex,
            decimal networkIntegrityIndex,
            decimal pumpReadinessIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            TreatmentCapacityIndex = treatmentCapacityIndex;
            NetworkIntegrityIndex = networkIntegrityIndex;
            PumpReadinessIndex = pumpReadinessIndex;
            CrewReadinessIndex = crewReadinessIndex;
            IncidentPressureIndex = incidentPressureIndex;
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal TreatmentCapacityIndex { get; private set; }
        public decimal NetworkIntegrityIndex { get; private set; }
        public decimal PumpReadinessIndex { get; private set; }
        public decimal CrewReadinessIndex { get; private set; }
        public decimal IncidentPressureIndex { get; private set; }
        public bool EmergencyModeEnabled { get; private set; }

        public static CityWaterDistributionInfrastructureState Create(
            CityWaterDistributionInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CityWaterDistributionInfrastructureState(
                treatmentCapacityIndex: snapshot.TreatmentCapacityIndex,
                networkIntegrityIndex: snapshot.NetworkIntegrityIndex,
                pumpReadinessIndex: snapshot.PumpReadinessIndex,
                crewReadinessIndex: snapshot.CrewReadinessIndex,
                incidentPressureIndex: snapshot.IncidentPressureIndex,
                emergencyModeEnabled: snapshot.EmergencyModeEnabled);
        }

        public void ApplySnapshot(CityWaterDistributionInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            TreatmentCapacityIndex = snapshot.TreatmentCapacityIndex;
            NetworkIntegrityIndex = snapshot.NetworkIntegrityIndex;
            PumpReadinessIndex = snapshot.PumpReadinessIndex;
            CrewReadinessIndex = snapshot.CrewReadinessIndex;
            IncidentPressureIndex = snapshot.IncidentPressureIndex;
            EmergencyModeEnabled = snapshot.EmergencyModeEnabled;
        }

        public void SetEmergencyMode(bool enabled)
        {
            EmergencyModeEnabled = enabled;
        }

        public void DispatchMaintenance(
            WaterDistributionMaintenanceFocus focus,
            WaterDistributionMaintenanceIntensity intensity)
        {
            decimal intensityFactor = intensity switch
            {
                WaterDistributionMaintenanceIntensity.Light => 0.45m,
                WaterDistributionMaintenanceIntensity.Standard => 0.72m,
                WaterDistributionMaintenanceIntensity.Heavy => 0.95m,
                _ => throw new ArgumentOutOfRangeException(nameof(intensity))
            };

            decimal treatmentBoost = focus switch
            {
                WaterDistributionMaintenanceFocus.Balanced => 0.0700m,
                WaterDistributionMaintenanceFocus.TreatmentStabilization => 0.1400m,
                WaterDistributionMaintenanceFocus.NetworkRepairs => 0.0400m,
                WaterDistributionMaintenanceFocus.PumpRecovery => 0.0450m,
                WaterDistributionMaintenanceFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal networkBoost = focus switch
            {
                WaterDistributionMaintenanceFocus.Balanced => 0.0600m,
                WaterDistributionMaintenanceFocus.TreatmentStabilization => 0.0300m,
                WaterDistributionMaintenanceFocus.NetworkRepairs => 0.1350m,
                WaterDistributionMaintenanceFocus.PumpRecovery => 0.0400m,
                WaterDistributionMaintenanceFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal pumpBoost = focus switch
            {
                WaterDistributionMaintenanceFocus.Balanced => 0.0550m,
                WaterDistributionMaintenanceFocus.TreatmentStabilization => 0.0400m,
                WaterDistributionMaintenanceFocus.NetworkRepairs => 0.0450m,
                WaterDistributionMaintenanceFocus.PumpRecovery => 0.1300m,
                WaterDistributionMaintenanceFocus.CrewRecovery => 0.0250m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal incidentRelief = focus switch
            {
                WaterDistributionMaintenanceFocus.Balanced => 0.0600m,
                WaterDistributionMaintenanceFocus.TreatmentStabilization => 0.0750m,
                WaterDistributionMaintenanceFocus.NetworkRepairs => 0.0800m,
                WaterDistributionMaintenanceFocus.PumpRecovery => 0.0700m,
                WaterDistributionMaintenanceFocus.CrewRecovery => 0.0400m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal crewDelta = focus switch
            {
                WaterDistributionMaintenanceFocus.Balanced => -0.0400m,
                WaterDistributionMaintenanceFocus.TreatmentStabilization => -0.0450m,
                WaterDistributionMaintenanceFocus.NetworkRepairs => -0.0550m,
                WaterDistributionMaintenanceFocus.PumpRecovery => -0.0500m,
                WaterDistributionMaintenanceFocus.CrewRecovery => 0.0650m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };

            TreatmentCapacityIndex = ClampAndRound(TreatmentCapacityIndex + (treatmentBoost * intensityFactor));
            NetworkIntegrityIndex = ClampAndRound(NetworkIntegrityIndex + (networkBoost * intensityFactor));
            PumpReadinessIndex = ClampAndRound(PumpReadinessIndex + (pumpBoost * intensityFactor));
            IncidentPressureIndex = ClampAndRound(IncidentPressureIndex - (incidentRelief * intensityFactor));
            CrewReadinessIndex = ClampAndRound(
                CrewReadinessIndex +
                (crewDelta * intensityFactor) +
                (EmergencyModeEnabled
                    ? -0.0200m
                    : 0.0200m));
        }

        public CityWaterDistributionInfrastructureSnapshot ToSnapshot()
        {
            return new CityWaterDistributionInfrastructureSnapshot(
                treatmentCapacityIndex: TreatmentCapacityIndex,
                networkIntegrityIndex: NetworkIntegrityIndex,
                pumpReadinessIndex: PumpReadinessIndex,
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
