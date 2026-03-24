using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed class CityHeatingInfrastructureSnapshot
    {
        public CityHeatingInfrastructureSnapshot(
            decimal plantCapacityIndex,
            decimal networkIntegrityIndex,
            decimal controlReadinessIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            PlantCapacityIndex = NormalizeIndex(
                value: plantCapacityIndex,
                paramName: nameof(plantCapacityIndex));
            NetworkIntegrityIndex = NormalizeIndex(
                value: networkIntegrityIndex,
                paramName: nameof(networkIntegrityIndex));
            ControlReadinessIndex = NormalizeIndex(
                value: controlReadinessIndex,
                paramName: nameof(controlReadinessIndex));
            CrewReadinessIndex = NormalizeIndex(
                value: crewReadinessIndex,
                paramName: nameof(crewReadinessIndex));
            IncidentPressureIndex = NormalizeIndex(
                value: incidentPressureIndex,
                paramName: nameof(incidentPressureIndex));
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal PlantCapacityIndex { get; }
        public decimal NetworkIntegrityIndex { get; }
        public decimal ControlReadinessIndex { get; }
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
