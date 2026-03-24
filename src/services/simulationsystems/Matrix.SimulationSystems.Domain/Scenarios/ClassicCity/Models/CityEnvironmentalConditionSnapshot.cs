using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    /// <summary>
    ///     Aggregate snapshot of the first environmental systems slice for Classic City.
    /// </summary>
    public sealed class CityEnvironmentalConditionSnapshot
    {
        public CityEnvironmentalConditionSnapshot(
            CitySystemSnapshot drainage,
            CityDrainageInfrastructureSnapshot drainageInfrastructure,
            CitySystemSnapshot snowRemoval,
            CitySnowRemovalInfrastructureSnapshot snowRemovalInfrastructure,
            CitySystemSnapshot roadAccess,
            CityRoadAccessInfrastructureSnapshot roadAccessInfrastructure,
            CitySystemSnapshot heating,
            CityHeatingInfrastructureSnapshot heatingInfrastructure,
            FloodingIndex floodingIndex,
            SnowAccumulationIndex snowAccumulationIndex,
            RoadAccessibilityIndex roadAccessibilityIndex,
            HeatingCoverageIndex heatingCoverageIndex,
            DateTimeOffset evaluatedAtUtc)
        {
            Drainage = GuardHelper.AgainstNull(
                value: drainage,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "drainage",
                    propertyName: nameof(drainage)));
            DrainageInfrastructure = GuardHelper.AgainstNull(
                value: drainageInfrastructure,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired(
                    propertyName: nameof(drainageInfrastructure)));
            SnowRemoval = GuardHelper.AgainstNull(
                value: snowRemoval,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "snowRemoval",
                    propertyName: nameof(snowRemoval)));
            SnowRemovalInfrastructure = GuardHelper.AgainstNull(
                value: snowRemovalInfrastructure,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired(
                    propertyName: nameof(snowRemovalInfrastructure)));
            RoadAccess = GuardHelper.AgainstNull(
                value: roadAccess,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "roadAccess",
                    propertyName: nameof(roadAccess)));
            RoadAccessInfrastructure = GuardHelper.AgainstNull(
                value: roadAccessInfrastructure,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired(
                    propertyName: nameof(roadAccessInfrastructure)));
            Heating = GuardHelper.AgainstNull(
                value: heating,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "heating",
                    propertyName: nameof(heating)));
            HeatingInfrastructure = GuardHelper.AgainstNull(
                value: heatingInfrastructure,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired(
                    propertyName: nameof(heatingInfrastructure)));
            FloodingIndex = floodingIndex;
            SnowAccumulationIndex = snowAccumulationIndex;
            RoadAccessibilityIndex = roadAccessibilityIndex;
            HeatingCoverageIndex = heatingCoverageIndex;
            EvaluatedAtUtc = EnsureUtc(
                value: evaluatedAtUtc,
                paramName: nameof(evaluatedAtUtc));
        }

        public CitySystemSnapshot Drainage { get; }
        public CityDrainageInfrastructureSnapshot DrainageInfrastructure { get; }
        public CitySystemSnapshot SnowRemoval { get; }
        public CitySnowRemovalInfrastructureSnapshot SnowRemovalInfrastructure { get; }
        public CitySystemSnapshot RoadAccess { get; }
        public CityRoadAccessInfrastructureSnapshot RoadAccessInfrastructure { get; }
        public CitySystemSnapshot Heating { get; }
        public CityHeatingInfrastructureSnapshot HeatingInfrastructure { get; }
        public FloodingIndex FloodingIndex { get; }
        public SnowAccumulationIndex SnowAccumulationIndex { get; }
        public RoadAccessibilityIndex RoadAccessibilityIndex { get; }
        public HeatingCoverageIndex HeatingCoverageIndex { get; }
        public DateTimeOffset EvaluatedAtUtc { get; }

        private static DateTimeOffset EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : throw ClassicCityDomainErrorsFactory.CityEnvironmentalTimestampMustBeUtc(
                    value: value,
                    propertyName: paramName);
        }
    }
}
