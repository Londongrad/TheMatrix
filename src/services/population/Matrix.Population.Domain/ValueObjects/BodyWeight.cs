using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public record class BodyWeight
    {
        public const decimal MinWeight = 2m;
        public const decimal MaxWeight = 250m;

        public decimal Kilograms { get; }

        public BodyWeight(decimal kilograms)
        {
            Kilograms = GuardHelper.AgainstOutOfRange(kilograms, MinWeight, MaxWeight, nameof(BodyWeight));
        }

        public static BodyWeight FromKilograms(decimal kilograms) => new(kilograms);

        public override string ToString() => $"{Kilograms:0.#} kg";
    }
}
