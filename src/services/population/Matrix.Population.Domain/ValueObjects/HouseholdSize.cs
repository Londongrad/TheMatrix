using Matrix.Population.Domain.Errors;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct HouseholdSize
    {
        public const int Min = 1;
        public const int Max = 12;

        private HouseholdSize(int value)
        {
            if (value < Min || value > Max)
                throw DomainErrorsFactory.HouseholdSizeOutOfRange(nameof(HouseholdSize));

            Value = value;
        }

        public int Value { get; }

        public static HouseholdSize From(int value)
        {
            return new HouseholdSize(value);
        }
    }
}
