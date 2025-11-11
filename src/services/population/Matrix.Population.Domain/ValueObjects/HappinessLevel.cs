using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly struct HappinessLevel(int value)
    {
        public int Value { get; } = GuardHelper.AgainstOutOfRange(value, 0, 100, nameof(Value));

        public static HappinessLevel From(int value) => new(value);

        public HappinessLevel WithDelta(int delta)
            => From(Math.Clamp(Value + delta, 0, 100));
    }
}
