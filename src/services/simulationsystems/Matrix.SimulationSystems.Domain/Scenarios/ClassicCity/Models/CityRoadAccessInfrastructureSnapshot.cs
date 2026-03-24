using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed class CityRoadAccessInfrastructureSnapshot
    {
        public CityRoadAccessInfrastructureSnapshot(
            decimal corridorAvailabilityIndex,
            decimal surfaceIntegrityIndex,
            decimal trafficControlReadinessIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            CorridorAvailabilityIndex = NormalizeIndex(
                value: corridorAvailabilityIndex,
                paramName: nameof(corridorAvailabilityIndex));
            SurfaceIntegrityIndex = NormalizeIndex(
                value: surfaceIntegrityIndex,
                paramName: nameof(surfaceIntegrityIndex));
            TrafficControlReadinessIndex = NormalizeIndex(
                value: trafficControlReadinessIndex,
                paramName: nameof(trafficControlReadinessIndex));
            CrewReadinessIndex = NormalizeIndex(
                value: crewReadinessIndex,
                paramName: nameof(crewReadinessIndex));
            IncidentPressureIndex = NormalizeIndex(
                value: incidentPressureIndex,
                paramName: nameof(incidentPressureIndex));
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal CorridorAvailabilityIndex { get; }
        public decimal SurfaceIntegrityIndex { get; }
        public decimal TrafficControlReadinessIndex { get; }
        public decimal CrewReadinessIndex { get; }
        public decimal IncidentPressureIndex { get; }
        public bool EmergencyModeEnabled { get; }

        private static decimal NormalizeIndex(
            decimal value,
            string paramName)
        {
            return decimal.Round(
                d: GuardHelper.AgainstOutOfRange(
                    value: value,
                    min: 0m,
                    max: 1m,
                    errorFactory: ClassicCityDomainErrorsFactory.CityNormalizedIndexOutOfRange,
                    propertyName: paramName),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
