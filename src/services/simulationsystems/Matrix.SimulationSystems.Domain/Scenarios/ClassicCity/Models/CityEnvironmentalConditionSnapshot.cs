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
            Drainage = drainage ?? throw new ArgumentNullException(nameof(drainage));
            SnowRemoval = snowRemoval ?? throw new ArgumentNullException(nameof(snowRemoval));
            RoadAccess = roadAccess ?? throw new ArgumentNullException(nameof(roadAccess));
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
                : throw new ArgumentException(
                    message: "Timestamp must be UTC.",
                    paramName: paramName);
        }
    }
}
