using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed class CityWaterDistributionInfrastructureSnapshot
    {
        public CityWaterDistributionInfrastructureSnapshot(
            decimal treatmentCapacityIndex,
            decimal networkIntegrityIndex,
            decimal pumpReadinessIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            TreatmentCapacityIndex = NormalizeIndex(
                value: treatmentCapacityIndex,
                paramName: nameof(treatmentCapacityIndex));
            NetworkIntegrityIndex = NormalizeIndex(
                value: networkIntegrityIndex,
                paramName: nameof(networkIntegrityIndex));
            PumpReadinessIndex = NormalizeIndex(
                value: pumpReadinessIndex,
                paramName: nameof(pumpReadinessIndex));
            CrewReadinessIndex = NormalizeIndex(
                value: crewReadinessIndex,
                paramName: nameof(crewReadinessIndex));
            IncidentPressureIndex = NormalizeIndex(
                value: incidentPressureIndex,
                paramName: nameof(incidentPressureIndex));
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal TreatmentCapacityIndex { get; }
        public decimal NetworkIntegrityIndex { get; }
        public decimal PumpReadinessIndex { get; }
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
