using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;

namespace Matrix.CityCore.Domain.Time
{
    /// <summary>
    ///     Simulation speed multiplier.
    ///     Example: 1.0 = real-time, 10.0 = 10x faster.
    /// </summary>
    public readonly record struct SimSpeed(decimal Multiplier)
    {
        public const decimal Min = 0.1m;
        public const decimal Max = 10_000m;

        public static SimSpeed RealTime()
        {
            return new SimSpeed(1.0m);
        }

        public static SimSpeed From(decimal multiplier)
        {
            GuardHelper.AgainstOutOfRange(
                value: multiplier,
                min: Min,
                max: Max,
                errorFactory: DomainErrorsFactory.SimSpeedMultiplierOutOfRange);

            return new SimSpeed(multiplier);
        }

        public TimeSpan Apply(TimeSpan realDelta)
        {
            GuardHelper.Ensure(
                condition: realDelta > TimeSpan.Zero,
                value: realDelta,
                errorFactory: DomainErrorsFactory.SimSpeedRealDeltaMustBePositive);

            // Convert using ticks to avoid TimeSpan limitations on fractional seconds.
            long scaledTicks = checked((long)decimal.Round(
                d: realDelta.Ticks * Multiplier,
                decimals: 0,
                mode: MidpointRounding.AwayFromZero));

            return TimeSpan.FromTicks(scaledTicks);
        }

        public override string ToString()
        {
            return Multiplier.ToString("0.###");
        }
    }
}
