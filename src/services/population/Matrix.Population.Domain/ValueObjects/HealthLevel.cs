using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct HealthLevel
    {
        public const int MinHealth = 0;
        public const int MaxHealth = 100;

        public HealthLevel(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: MinHealth,
                max: MaxHealth,
                propertyName: nameof(HealthLevel));
        }

        public int Value { get; }

        public static HealthLevel From(int value)
        {
            return new HealthLevel(value);
        }

        public static HealthLevel Default()
        {
            return new HealthLevel(MaxHealth);
            // здоров как бык
        }

        public HealthLevel WithDelta(int delta)
        {
            int newValue = Math.Clamp(
                value: Value + delta,
                min: MinHealth,
                max: MaxHealth);
            return new HealthLevel(newValue);
        }
    }
}
