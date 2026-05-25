using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CitySnowRemovalInfrastructureState
    {
        private CitySnowRemovalInfrastructureState() { }

        private CitySnowRemovalInfrastructureState(
            decimal fleetAvailabilityIndex,
            decimal routeCoverageIndex,
            decimal deicingReadinessIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            FleetAvailabilityIndex = fleetAvailabilityIndex;
            RouteCoverageIndex = routeCoverageIndex;
            DeicingReadinessIndex = deicingReadinessIndex;
            CrewReadinessIndex = crewReadinessIndex;
            IncidentPressureIndex = incidentPressureIndex;
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal FleetAvailabilityIndex { get; private set; }
        public decimal RouteCoverageIndex { get; private set; }
        public decimal DeicingReadinessIndex { get; private set; }
        public decimal CrewReadinessIndex { get; private set; }
        public decimal IncidentPressureIndex { get; private set; }
        public bool EmergencyModeEnabled { get; private set; }

        public static CitySnowRemovalInfrastructureState Create(CitySnowRemovalInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CitySnowRemovalInfrastructureState(
                fleetAvailabilityIndex: snapshot.FleetAvailabilityIndex,
                routeCoverageIndex: snapshot.RouteCoverageIndex,
                deicingReadinessIndex: snapshot.DeicingReadinessIndex,
                crewReadinessIndex: snapshot.CrewReadinessIndex,
                incidentPressureIndex: snapshot.IncidentPressureIndex,
                emergencyModeEnabled: snapshot.EmergencyModeEnabled);
        }

        public void ApplySnapshot(CitySnowRemovalInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            FleetAvailabilityIndex = snapshot.FleetAvailabilityIndex;
            RouteCoverageIndex = snapshot.RouteCoverageIndex;
            DeicingReadinessIndex = snapshot.DeicingReadinessIndex;
            CrewReadinessIndex = snapshot.CrewReadinessIndex;
            IncidentPressureIndex = snapshot.IncidentPressureIndex;
            EmergencyModeEnabled = snapshot.EmergencyModeEnabled;
        }

        public void SetEmergencyMode(bool enabled)
        {
            EmergencyModeEnabled = enabled;
        }

        public void DispatchMaintenance(
            SnowRemovalMaintenanceFocus focus,
            SnowRemovalMaintenanceIntensity intensity)
        {
            decimal intensityFactor = intensity switch
            {
                SnowRemovalMaintenanceIntensity.Light => 0.45m,
                SnowRemovalMaintenanceIntensity.Standard => 0.72m,
                SnowRemovalMaintenanceIntensity.Heavy => 0.95m,
                _ => throw new ArgumentOutOfRangeException(nameof(intensity))
            };

            decimal fleetBoost = focus switch
            {
                SnowRemovalMaintenanceFocus.Balanced => 0.0600m,
                SnowRemovalMaintenanceFocus.FleetRepairs => 0.1200m,
                SnowRemovalMaintenanceFocus.RouteClearance => 0.0450m,
                SnowRemovalMaintenanceFocus.DeicingCalibration => 0.0350m,
                SnowRemovalMaintenanceFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal routeBoost = focus switch
            {
                SnowRemovalMaintenanceFocus.Balanced => 0.0750m,
                SnowRemovalMaintenanceFocus.FleetRepairs => 0.0350m,
                SnowRemovalMaintenanceFocus.RouteClearance => 0.1350m,
                SnowRemovalMaintenanceFocus.DeicingCalibration => 0.0550m,
                SnowRemovalMaintenanceFocus.CrewRecovery => 0.0350m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal deicingBoost = focus switch
            {
                SnowRemovalMaintenanceFocus.Balanced => 0.0650m,
                SnowRemovalMaintenanceFocus.FleetRepairs => 0.0250m,
                SnowRemovalMaintenanceFocus.RouteClearance => 0.0550m,
                SnowRemovalMaintenanceFocus.DeicingCalibration => 0.1250m,
                SnowRemovalMaintenanceFocus.CrewRecovery => 0.0400m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal incidentRelief = focus switch
            {
                SnowRemovalMaintenanceFocus.Balanced => 0.0600m,
                SnowRemovalMaintenanceFocus.FleetRepairs => 0.0500m,
                SnowRemovalMaintenanceFocus.RouteClearance => 0.0750m,
                SnowRemovalMaintenanceFocus.DeicingCalibration => 0.0650m,
                SnowRemovalMaintenanceFocus.CrewRecovery => 0.0450m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal crewDelta = focus switch
            {
                SnowRemovalMaintenanceFocus.Balanced => -0.0400m,
                SnowRemovalMaintenanceFocus.FleetRepairs => -0.0500m,
                SnowRemovalMaintenanceFocus.RouteClearance => -0.0600m,
                SnowRemovalMaintenanceFocus.DeicingCalibration => -0.0450m,
                SnowRemovalMaintenanceFocus.CrewRecovery => 0.0650m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };

            FleetAvailabilityIndex = ClampAndRound(FleetAvailabilityIndex + (fleetBoost * intensityFactor));
            RouteCoverageIndex = ClampAndRound(RouteCoverageIndex + (routeBoost * intensityFactor));
            DeicingReadinessIndex = ClampAndRound(DeicingReadinessIndex + (deicingBoost * intensityFactor));
            IncidentPressureIndex = ClampAndRound(IncidentPressureIndex - (incidentRelief * intensityFactor));
            CrewReadinessIndex = ClampAndRound(
                CrewReadinessIndex +
                (crewDelta * intensityFactor) +
                (EmergencyModeEnabled
                    ? -0.0200m
                    : 0.0200m));
        }

        public CitySnowRemovalInfrastructureSnapshot ToSnapshot()
        {
            return new CitySnowRemovalInfrastructureSnapshot(
                fleetAvailabilityIndex: FleetAvailabilityIndex,
                routeCoverageIndex: RouteCoverageIndex,
                deicingReadinessIndex: DeicingReadinessIndex,
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
