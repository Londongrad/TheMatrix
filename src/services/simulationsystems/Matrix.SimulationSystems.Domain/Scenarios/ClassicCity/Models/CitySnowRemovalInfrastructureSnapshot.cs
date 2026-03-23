using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed class CitySnowRemovalInfrastructureSnapshot
    {
        public CitySnowRemovalInfrastructureSnapshot(
            decimal fleetAvailabilityIndex,
            decimal routeCoverageIndex,
            decimal deicingReadinessIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            FleetAvailabilityIndex = NormalizeIndex(
                value: fleetAvailabilityIndex,
                paramName: nameof(fleetAvailabilityIndex));
            RouteCoverageIndex = NormalizeIndex(
                value: routeCoverageIndex,
                paramName: nameof(routeCoverageIndex));
            DeicingReadinessIndex = NormalizeIndex(
                value: deicingReadinessIndex,
                paramName: nameof(deicingReadinessIndex));
            CrewReadinessIndex = NormalizeIndex(
                value: crewReadinessIndex,
                paramName: nameof(crewReadinessIndex));
            IncidentPressureIndex = NormalizeIndex(
                value: incidentPressureIndex,
                paramName: nameof(incidentPressureIndex));
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal FleetAvailabilityIndex { get; }
        public decimal RouteCoverageIndex { get; }
        public decimal DeicingReadinessIndex { get; }
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
