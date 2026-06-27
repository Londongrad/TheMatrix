using Matrix.BuildingBlocks.Domain;

namespace Matrix.Healthcare.Domain.Patients
{
    public readonly record struct HealthScore
    {
        public const int Minimum = 0;
        public const int Maximum = 100;

        public HealthScore(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: Minimum,
                max: Maximum,
                propertyName: nameof(HealthScore));
        }

        public int Value { get; }

        public static HealthScore Full => new(Maximum);

        public HealthScore ApplyDelta(int delta)
        {
            return new HealthScore(Math.Clamp(Value + delta, Minimum, Maximum));
        }
    }
}
