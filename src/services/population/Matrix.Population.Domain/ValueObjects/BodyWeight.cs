using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly struct BodyWeight(decimal kilograms)
    {
        public decimal Kilograms { get; } = GuardHelper.AgainstOutOfRange(kilograms, 2m, 400m, nameof(BodyWeight));

        public static BodyWeight FromKilograms(decimal kilograms) => new(kilograms);

        public override string ToString() => $"{Kilograms:0.#} kg";
    }
}
