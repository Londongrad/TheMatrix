using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed class CityDrainageInfrastructureSnapshot
    {
        public CityDrainageInfrastructureSnapshot(
            decimal pumpCapacityIndex,
            decimal networkIntegrityIndex,
            decimal blockageIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            PumpCapacityIndex = NormalizeIndex(
                value: pumpCapacityIndex,
                paramName: nameof(pumpCapacityIndex));
            NetworkIntegrityIndex = NormalizeIndex(
                value: networkIntegrityIndex,
                paramName: nameof(networkIntegrityIndex));
            BlockageIndex = NormalizeIndex(
                value: blockageIndex,
                paramName: nameof(blockageIndex));
            CrewReadinessIndex = NormalizeIndex(
                value: crewReadinessIndex,
                paramName: nameof(crewReadinessIndex));
            IncidentPressureIndex = NormalizeIndex(
                value: incidentPressureIndex,
                paramName: nameof(incidentPressureIndex));
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal PumpCapacityIndex { get; }
        public decimal NetworkIntegrityIndex { get; }
        public decimal BlockageIndex { get; }
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
