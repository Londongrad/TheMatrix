using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed class CitySanitationInfrastructureSnapshot
    {
        public CitySanitationInfrastructureSnapshot(
            decimal treatmentStabilityIndex,
            decimal networkIntegrityIndex,
            decimal overflowControlIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            TreatmentStabilityIndex = NormalizeIndex(
                value: treatmentStabilityIndex,
                paramName: nameof(treatmentStabilityIndex));
            NetworkIntegrityIndex = NormalizeIndex(
                value: networkIntegrityIndex,
                paramName: nameof(networkIntegrityIndex));
            OverflowControlIndex = NormalizeIndex(
                value: overflowControlIndex,
                paramName: nameof(overflowControlIndex));
            CrewReadinessIndex = NormalizeIndex(
                value: crewReadinessIndex,
                paramName: nameof(crewReadinessIndex));
            IncidentPressureIndex = NormalizeIndex(
                value: incidentPressureIndex,
                paramName: nameof(incidentPressureIndex));
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal TreatmentStabilityIndex { get; }
        public decimal NetworkIntegrityIndex { get; }
        public decimal OverflowControlIndex { get; }
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
