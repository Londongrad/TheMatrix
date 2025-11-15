using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly struct HealthLevel(int value)
    {
        public int Value { get; } = GuardHelper.AgainstOutOfRange(value, 0, 100, nameof(HealthLevel));

        public static HealthLevel From(int value) => new(value);

        public static HealthLevel Default() => new(100); // здоров как бык

        public HealthLevel WithDelta(int delta)
        {
            var newValue = Math.Clamp(Value + delta, 0, 100);
            return new HealthLevel(newValue);
        }

        public bool IsDead => Value == 0;
    }
}
