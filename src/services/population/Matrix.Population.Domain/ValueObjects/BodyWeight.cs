using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public record class BodyWeight
    {
        public const decimal MinWeight = 2m;
        public const decimal MaxWeight = 250m;

        public BodyWeight(decimal kilograms)
        {
            Kilograms = GuardHelper.AgainstOutOfRange(
                value: kilograms,
                min: MinWeight,
                max: MaxWeight,
                propertyName: nameof(BodyWeight));
        }

        public decimal Kilograms { get; }

        public static BodyWeight FromKilograms(decimal kilograms)
        {
            return new BodyWeight(kilograms);
        }

        public override string ToString()
        {
            return $"{Kilograms:0.#} kg";
        }
    }
}
