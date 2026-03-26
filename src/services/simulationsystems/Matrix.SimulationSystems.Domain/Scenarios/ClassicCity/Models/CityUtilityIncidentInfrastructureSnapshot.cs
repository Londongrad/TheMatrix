using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    public sealed class CityUtilityIncidentInfrastructureSnapshot
    {
        public CityUtilityIncidentInfrastructureSnapshot(
            decimal dispatchReadinessIndex,
            decimal restorationCoverageIndex,
            decimal spareCapacityIndex,
            decimal fieldCoordinationIndex,
            decimal incidentQueuePressureIndex,
            bool emergencyModeEnabled)
        {
            DispatchReadinessIndex = NormalizeIndex(
                value: dispatchReadinessIndex,
                paramName: nameof(dispatchReadinessIndex));
            RestorationCoverageIndex = NormalizeIndex(
                value: restorationCoverageIndex,
                paramName: nameof(restorationCoverageIndex));
            SpareCapacityIndex = NormalizeIndex(
                value: spareCapacityIndex,
                paramName: nameof(spareCapacityIndex));
            FieldCoordinationIndex = NormalizeIndex(
                value: fieldCoordinationIndex,
                paramName: nameof(fieldCoordinationIndex));
            IncidentQueuePressureIndex = NormalizeIndex(
                value: incidentQueuePressureIndex,
                paramName: nameof(incidentQueuePressureIndex));
            EmergencyModeEnabled = emergencyModeEnabled;
        }

        public decimal DispatchReadinessIndex { get; }
        public decimal RestorationCoverageIndex { get; }
        public decimal SpareCapacityIndex { get; }
        public decimal FieldCoordinationIndex { get; }
        public decimal IncidentQueuePressureIndex { get; }
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
