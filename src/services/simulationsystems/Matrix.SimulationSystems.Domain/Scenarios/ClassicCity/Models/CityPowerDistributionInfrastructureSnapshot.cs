using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed class CityPowerDistributionInfrastructureSnapshot
    {
        public CityPowerDistributionInfrastructureSnapshot(
            decimal substationCapacityIndex,
            decimal gridIntegrityIndex,
            decimal switchingReadinessIndex,
            decimal crewReadinessIndex,
            decimal incidentPressureIndex,
            bool emergencyModeEnabled)
        {
            SubstationCapacityIndex = NormalizeIndex(
                value: substationCapacityIndex,
                paramName: nameof(substationCapacityIndex));
            GridIntegrityIndex = NormalizeIndex(
                value: gridIntegrityIndex,
                paramName: nameof(gridIntegrityIndex));
            SwitchingReadinessIndex = NormalizeIndex(
                value: switchingReadinessIndex,
                paramName: nameof(switchingReadinessIndex));
            CrewReadinessIndex = NormalizeIndex(
                value: crewReadinessIndex,
                paramName: nameof(crewReadinessIndex));
            IncidentPressureIndex = NormalizeIndex(
                value: incidentPressureIndex,
                paramName: nameof(incidentPressureIndex));
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal SubstationCapacityIndex { get; }
        public decimal GridIntegrityIndex { get; }
        public decimal SwitchingReadinessIndex { get; }
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
