using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct HappinessLevel
    {
        public const int MinHappiness = 0;
        public const int MaxHappiness = 100;

        public int Value { get; }

        public HappinessLevel(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(value, MinHappiness, MaxHappiness, nameof(HappinessLevel));
        }

        public static HappinessLevel From(int value) => new(value);

        public HappinessLevel WithDelta(int delta)
            => From(Math.Clamp(Value + delta, MinHappiness, MaxHappiness));

        /// <summary> Default is 50. </summary>
        public static HappinessLevel Default()
            => From(50);
    }
}
