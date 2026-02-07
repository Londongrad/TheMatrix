using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct StressLevel
    {
        public const int MinStress = 0;
        public const int MaxStress = 100;

        public StressLevel(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: MinStress,
                max: MaxStress,
                propertyName: nameof(StressLevel));
        }

        public int Value { get; }

        public static StressLevel From(int value)
        {
            return new StressLevel(value);
        }

        public static StressLevel Default()
        {
            return From(25);
        }

        public StressLevel WithDelta(int delta)
        {
            return From(
                Math.Clamp(
                    value: Value + delta,
                    min: MinStress,
                    max: MaxStress));
        }
    }
}
