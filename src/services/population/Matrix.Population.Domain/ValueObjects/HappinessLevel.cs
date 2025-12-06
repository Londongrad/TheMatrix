using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct HappinessLevel
    {
        public const int MinHappiness = 0;
        public const int MaxHappiness = 100;

        public HappinessLevel(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(value: value, min: MinHappiness, max: MaxHappiness,
                propertyName: nameof(HappinessLevel));
        }

        public int Value { get; }

        public static HappinessLevel From(int value) => new(value);

        public HappinessLevel WithDelta(int delta)
            => From(Math.Clamp(value: Value + delta, min: MinHappiness, max: MaxHappiness));

        /// <summary> Default is 50. </summary>
        public static HappinessLevel Default()
            => From(50);
    }
}
