using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct HealthLevel
    {
        public const int MinHealth = 0;
        public const int MaxHealth = 100;

        public int Value { get; }

        public HealthLevel(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(value, MinHealth, MaxHealth, nameof(HealthLevel));
        }

        public static HealthLevel From(int value) => new(value);

        public static HealthLevel Default() => new(MaxHealth); // здоров как бык

        public HealthLevel WithDelta(int delta)
        {
            var newValue = Math.Clamp(Value + delta, MinHealth, MaxHealth);
            return new HealthLevel(newValue);
        }
    }
}
