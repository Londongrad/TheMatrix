using Matrix.BuildingBlocks.Domain;

namespace Matrix.Healthcare.Domain.Patients
{
    public readonly record struct FunctionalCapacityScore
    {
        public const int Minimum = 0;
        public const int Maximum = 100;

        public FunctionalCapacityScore(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: Minimum,
                max: Maximum,
                propertyName: nameof(FunctionalCapacityScore));
        }

        public int Value { get; }

        public static FunctionalCapacityScore Full => new(Maximum);
    }
}
