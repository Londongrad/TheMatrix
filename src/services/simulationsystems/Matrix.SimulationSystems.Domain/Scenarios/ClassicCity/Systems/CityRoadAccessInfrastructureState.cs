using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityRoadAccessInfrastructureState
    {
        private CityRoadAccessInfrastructureState() { }

        private CityRoadAccessInfrastructureState(
            decimal corridorAvailabilityIndex,
            decimal surfaceIntegrityIndex,
            decimal trafficControlReadinessIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            CorridorAvailabilityIndex = corridorAvailabilityIndex;
            SurfaceIntegrityIndex = surfaceIntegrityIndex;
            TrafficControlReadinessIndex = trafficControlReadinessIndex;
            CrewReadinessIndex = crewReadinessIndex;
            IncidentPressureIndex = incidentPressureIndex;
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal CorridorAvailabilityIndex { get; private set; }
        public decimal SurfaceIntegrityIndex { get; private set; }
        public decimal TrafficControlReadinessIndex { get; private set; }
        public decimal CrewReadinessIndex { get; private set; }
        public decimal IncidentPressureIndex { get; private set; }
        public bool EmergencyModeEnabled { get; private set; }

        public static CityRoadAccessInfrastructureState Create(
            CityRoadAccessInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CityRoadAccessInfrastructureState(
                corridorAvailabilityIndex: snapshot.CorridorAvailabilityIndex,
                surfaceIntegrityIndex: snapshot.SurfaceIntegrityIndex,
                trafficControlReadinessIndex: snapshot.TrafficControlReadinessIndex,
                crewReadinessIndex: snapshot.CrewReadinessIndex,
                incidentPressureIndex: snapshot.IncidentPressureIndex,
                emergencyModeEnabled: snapshot.EmergencyModeEnabled);
        }

        public void ApplySnapshot(CityRoadAccessInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            CorridorAvailabilityIndex = snapshot.CorridorAvailabilityIndex;
            SurfaceIntegrityIndex = snapshot.SurfaceIntegrityIndex;
            TrafficControlReadinessIndex = snapshot.TrafficControlReadinessIndex;
            CrewReadinessIndex = snapshot.CrewReadinessIndex;
            IncidentPressureIndex = snapshot.IncidentPressureIndex;
            EmergencyModeEnabled = snapshot.EmergencyModeEnabled;
        }

        public void SetEmergencyMode(bool enabled)
        {
            EmergencyModeEnabled = enabled;
        }

        public void DispatchMaintenance(
            RoadAccessMaintenanceFocus focus,
            RoadAccessMaintenanceIntensity intensity)
        {
            decimal intensityFactor = intensity switch
            {
                RoadAccessMaintenanceIntensity.Light => 0.45m,
                RoadAccessMaintenanceIntensity.Standard => 0.72m,
                RoadAccessMaintenanceIntensity.Heavy => 0.95m,
                _ => throw new ArgumentOutOfRangeException(nameof(intensity))
            };

            decimal corridorBoost = focus switch
            {
                RoadAccessMaintenanceFocus.Balanced => 0.0700m,
                RoadAccessMaintenanceFocus.CorridorClearance => 0.1400m,
                RoadAccessMaintenanceFocus.SurfaceRepairs => 0.0450m,
                RoadAccessMaintenanceFocus.TrafficControl => 0.0550m,
                RoadAccessMaintenanceFocus.CrewRecovery => 0.0300m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal surfaceBoost = focus switch
            {
                RoadAccessMaintenanceFocus.Balanced => 0.0600m,
                RoadAccessMaintenanceFocus.CorridorClearance => 0.0350m,
                RoadAccessMaintenanceFocus.SurfaceRepairs => 0.1300m,
                RoadAccessMaintenanceFocus.TrafficControl => 0.0450m,
                RoadAccessMaintenanceFocus.CrewRecovery => 0.0250m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal trafficBoost = focus switch
            {
                RoadAccessMaintenanceFocus.Balanced => 0.0550m,
                RoadAccessMaintenanceFocus.CorridorClearance => 0.0600m,
                RoadAccessMaintenanceFocus.SurfaceRepairs => 0.0350m,
                RoadAccessMaintenanceFocus.TrafficControl => 0.1300m,
                RoadAccessMaintenanceFocus.CrewRecovery => 0.0300m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal incidentRelief = focus switch
            {
                RoadAccessMaintenanceFocus.Balanced => 0.0600m,
                RoadAccessMaintenanceFocus.CorridorClearance => 0.0750m,
                RoadAccessMaintenanceFocus.SurfaceRepairs => 0.0550m,
                RoadAccessMaintenanceFocus.TrafficControl => 0.0700m,
                RoadAccessMaintenanceFocus.CrewRecovery => 0.0400m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal crewDelta = focus switch
            {
                RoadAccessMaintenanceFocus.Balanced => -0.0400m,
                RoadAccessMaintenanceFocus.CorridorClearance => -0.0600m,
                RoadAccessMaintenanceFocus.SurfaceRepairs => -0.0500m,
                RoadAccessMaintenanceFocus.TrafficControl => -0.0450m,
                RoadAccessMaintenanceFocus.CrewRecovery => 0.0650m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };

            CorridorAvailabilityIndex = ClampAndRound(CorridorAvailabilityIndex + (corridorBoost * intensityFactor));
            SurfaceIntegrityIndex = ClampAndRound(SurfaceIntegrityIndex + (surfaceBoost * intensityFactor));
            TrafficControlReadinessIndex = ClampAndRound(TrafficControlReadinessIndex + (trafficBoost * intensityFactor));
            IncidentPressureIndex = ClampAndRound(IncidentPressureIndex - (incidentRelief * intensityFactor));
            CrewReadinessIndex = ClampAndRound(
                CrewReadinessIndex + (crewDelta * intensityFactor) + (EmergencyModeEnabled ? -0.0200m : 0.0200m));
        }

        public CityRoadAccessInfrastructureSnapshot ToSnapshot()
        {
            return new CityRoadAccessInfrastructureSnapshot(
                corridorAvailabilityIndex: CorridorAvailabilityIndex,
                surfaceIntegrityIndex: SurfaceIntegrityIndex,
                trafficControlReadinessIndex: TrafficControlReadinessIndex,
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
