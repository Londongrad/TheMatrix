using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct EnergyLevel
    {
        public const int MinEnergy = 0;
        public const int MaxEnergy = 100;

        public EnergyLevel(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: MinEnergy,
                max: MaxEnergy,
                propertyName: nameof(EnergyLevel));
        }

        public int Value { get; }

        public static EnergyLevel From(int value)
        {
            return new EnergyLevel(value);
        }

        public static EnergyLevel Default()
        {
            return From(70);
        }

        public EnergyLevel WithDelta(int delta)
        {
            return From(
                Math.Clamp(
                    value: Value + delta,
                    min: MinEnergy,
                    max: MaxEnergy));
        }
    }
}
