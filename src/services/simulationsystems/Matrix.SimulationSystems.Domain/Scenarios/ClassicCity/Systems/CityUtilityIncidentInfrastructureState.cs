using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityUtilityIncidentInfrastructureState
    {
        private CityUtilityIncidentInfrastructureState() { }

        private CityUtilityIncidentInfrastructureState(
            decimal dispatchReadinessIndex,
            decimal restorationCoverageIndex,
            decimal spareCapacityIndex,
            decimal fieldCoordinationIndex,
            decimal incidentQueuePressureIndex,
            bool emergencyModeEnabled)
        {
            DispatchReadinessIndex = dispatchReadinessIndex;
            RestorationCoverageIndex = restorationCoverageIndex;
            SpareCapacityIndex = spareCapacityIndex;
            FieldCoordinationIndex = fieldCoordinationIndex;
            IncidentQueuePressureIndex = incidentQueuePressureIndex;
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal DispatchReadinessIndex { get; private set; }
        public decimal RestorationCoverageIndex { get; private set; }
        public decimal SpareCapacityIndex { get; private set; }
        public decimal FieldCoordinationIndex { get; private set; }
        public decimal IncidentQueuePressureIndex { get; private set; }
        public bool EmergencyModeEnabled { get; private set; }

        public static CityUtilityIncidentInfrastructureState Create(CityUtilityIncidentInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CityUtilityIncidentInfrastructureState(
                dispatchReadinessIndex: snapshot.DispatchReadinessIndex,
                restorationCoverageIndex: snapshot.RestorationCoverageIndex,
                spareCapacityIndex: snapshot.SpareCapacityIndex,
                fieldCoordinationIndex: snapshot.FieldCoordinationIndex,
                incidentQueuePressureIndex: snapshot.IncidentQueuePressureIndex,
                emergencyModeEnabled: snapshot.EmergencyModeEnabled);
        }

        public void ApplySnapshot(CityUtilityIncidentInfrastructureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            DispatchReadinessIndex = snapshot.DispatchReadinessIndex;
            RestorationCoverageIndex = snapshot.RestorationCoverageIndex;
            SpareCapacityIndex = snapshot.SpareCapacityIndex;
            FieldCoordinationIndex = snapshot.FieldCoordinationIndex;
            IncidentQueuePressureIndex = snapshot.IncidentQueuePressureIndex;
            EmergencyModeEnabled = snapshot.EmergencyModeEnabled;
        }

        public void SetEmergencyMode(bool enabled)
        {
            EmergencyModeEnabled = enabled;
        }

        public void DispatchResponse(
            UtilityIncidentResponseFocus focus,
            UtilityIncidentResponseIntensity intensity)
        {
            decimal intensityFactor = intensity switch
            {
                UtilityIncidentResponseIntensity.Light => 0.45m,
                UtilityIncidentResponseIntensity.Standard => 0.72m,
                UtilityIncidentResponseIntensity.Heavy => 0.95m,
                _ => throw new ArgumentOutOfRangeException(nameof(intensity))
            };

            decimal dispatchBoost = focus switch
            {
                UtilityIncidentResponseFocus.Balanced => 0.0700m,
                UtilityIncidentResponseFocus.PowerOutages => 0.0950m,
                UtilityIncidentResponseFocus.HeatingFailures => 0.0800m,
                UtilityIncidentResponseFocus.WaterLeaks => 0.0750m,
                UtilityIncidentResponseFocus.SanitationOverflows => 0.0750m,
                UtilityIncidentResponseFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal restorationBoost = focus switch
            {
                UtilityIncidentResponseFocus.Balanced => 0.0650m,
                UtilityIncidentResponseFocus.PowerOutages => 0.1100m,
                UtilityIncidentResponseFocus.HeatingFailures => 0.0900m,
                UtilityIncidentResponseFocus.WaterLeaks => 0.0950m,
                UtilityIncidentResponseFocus.SanitationOverflows => 0.0900m,
                UtilityIncidentResponseFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal spareCapacityBoost = focus switch
            {
                UtilityIncidentResponseFocus.Balanced => 0.0550m,
                UtilityIncidentResponseFocus.PowerOutages => 0.0600m,
                UtilityIncidentResponseFocus.HeatingFailures => 0.0500m,
                UtilityIncidentResponseFocus.WaterLeaks => 0.0650m,
                UtilityIncidentResponseFocus.SanitationOverflows => 0.0600m,
                UtilityIncidentResponseFocus.CrewRecovery => 0.0200m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal coordinationBoost = focus switch
            {
                UtilityIncidentResponseFocus.Balanced => 0.0600m,
                UtilityIncidentResponseFocus.PowerOutages => 0.0700m,
                UtilityIncidentResponseFocus.HeatingFailures => 0.0650m,
                UtilityIncidentResponseFocus.WaterLeaks => 0.0650m,
                UtilityIncidentResponseFocus.SanitationOverflows => 0.0600m,
                UtilityIncidentResponseFocus.CrewRecovery => 0.0300m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };
            decimal queueRelief = focus switch
            {
                UtilityIncidentResponseFocus.Balanced => 0.0700m,
                UtilityIncidentResponseFocus.PowerOutages => 0.0850m,
                UtilityIncidentResponseFocus.HeatingFailures => 0.0800m,
                UtilityIncidentResponseFocus.WaterLeaks => 0.0850m,
                UtilityIncidentResponseFocus.SanitationOverflows => 0.0800m,
                UtilityIncidentResponseFocus.CrewRecovery => 0.0350m,
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };

            DispatchReadinessIndex = ClampAndRound(DispatchReadinessIndex + (dispatchBoost * intensityFactor));
            RestorationCoverageIndex = ClampAndRound(RestorationCoverageIndex + (restorationBoost * intensityFactor));
            SpareCapacityIndex = ClampAndRound(SpareCapacityIndex + (spareCapacityBoost * intensityFactor));
            FieldCoordinationIndex = ClampAndRound(FieldCoordinationIndex + (coordinationBoost * intensityFactor));
            IncidentQueuePressureIndex = ClampAndRound(IncidentQueuePressureIndex - (queueRelief * intensityFactor));
        }

        public CityUtilityIncidentInfrastructureSnapshot ToSnapshot()
        {
            return new CityUtilityIncidentInfrastructureSnapshot(
                dispatchReadinessIndex: DispatchReadinessIndex,
                restorationCoverageIndex: RestorationCoverageIndex,
                spareCapacityIndex: SpareCapacityIndex,
                fieldCoordinationIndex: FieldCoordinationIndex,
                incidentQueuePressureIndex: IncidentQueuePressureIndex,
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
