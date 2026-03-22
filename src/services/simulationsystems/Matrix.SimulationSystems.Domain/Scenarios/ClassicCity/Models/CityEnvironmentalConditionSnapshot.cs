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
            CitySystemSnapshot snowRemoval,
            CitySystemSnapshot roadAccess,
            FloodingIndex floodingIndex,
            SnowAccumulationIndex snowAccumulationIndex,
            RoadAccessibilityIndex roadAccessibilityIndex,
            DateTimeOffset evaluatedAtUtc)
        {
            Drainage = GuardHelper.AgainstNull(
                value: drainage,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "drainage",
                    propertyName: nameof(drainage)));
            SnowRemoval = GuardHelper.AgainstNull(
                value: snowRemoval,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "snowRemoval",
                    propertyName: nameof(snowRemoval)));
            RoadAccess = GuardHelper.AgainstNull(
                value: roadAccess,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "roadAccess",
                    propertyName: nameof(roadAccess)));
            FloodingIndex = floodingIndex;
            SnowAccumulationIndex = snowAccumulationIndex;
            RoadAccessibilityIndex = roadAccessibilityIndex;
            EvaluatedAtUtc = EnsureUtc(
                value: evaluatedAtUtc,
                paramName: nameof(evaluatedAtUtc));
        }

        public CitySystemSnapshot Drainage { get; }
        public CitySystemSnapshot SnowRemoval { get; }
        public CitySystemSnapshot RoadAccess { get; }
        public FloodingIndex FloodingIndex { get; }
        public SnowAccumulationIndex SnowAccumulationIndex { get; }
        public RoadAccessibilityIndex RoadAccessibilityIndex { get; }
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
