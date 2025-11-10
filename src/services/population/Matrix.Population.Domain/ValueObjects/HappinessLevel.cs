using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct HappinessLevel
    {
        public int Value { get; }

        public HappinessLevel(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(value, 0, 100, nameof(HappinessLevel));
        }

        public static HappinessLevel From(int value) => new(value);

        public HappinessLevel WithDelta(int delta)
            => From(Math.Clamp(Value + delta, 0, 100));

        /// <summary> Default is 50. </summary>
        public static HappinessLevel Default()
            => From(50);
    }
}
