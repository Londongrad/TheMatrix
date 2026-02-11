using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities
{
    /// <summary>
    ///     User-facing seed value used for deterministic city generation.
    /// </summary>
    public readonly record struct CityGenerationSeed
    {
        public const int MaxLength = 128;

        public CityGenerationSeed(string? value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.CityGenerationSeedNullOrEmpty,
                trim: true,
                propertyName: nameof(Value));

            if (normalized.Length > MaxLength)
                throw ClassicCityDomainErrorsFactory.CityGenerationSeedTooLong(
                    value: normalized,
                    max: MaxLength,
                    propertyName: nameof(Value));

            Value = normalized;
        }

        public string Value { get; }

        public override string ToString()
        {
            return Value;
        }
    }
}
