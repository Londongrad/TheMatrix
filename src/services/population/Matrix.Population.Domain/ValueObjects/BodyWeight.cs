using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public record class BodyWeight
    {
        public decimal Kilograms { get; }

        public BodyWeight(decimal kilograms)
        {
            Kilograms = GuardHelper.AgainstOutOfRange(kilograms, 2m, 400m, nameof(BodyWeight));
        }

        public static BodyWeight FromKilograms(decimal kilograms) => new(kilograms);

        public override string ToString() => $"{Kilograms:0.#} kg";
    }
}
