using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct FunctionalCapacityLevel
    {
        public const int Minimum = 0;
        public const int Maximum = 100;

        public FunctionalCapacityLevel(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: Minimum,
                max: Maximum,
                propertyName: nameof(FunctionalCapacityLevel));
        }

        public int Value { get; }

        public static FunctionalCapacityLevel From(int value) => new(value);

        public static FunctionalCapacityLevel Full => new(Maximum);
    }
}
