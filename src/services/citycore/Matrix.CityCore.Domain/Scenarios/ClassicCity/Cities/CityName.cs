using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities
{
    /// <summary>
    ///     City display name. Not necessarily unique (uniqueness is typically enforced outside the domain).
    /// </summary>
    public readonly record struct CityName
    {
        public const int MaxLength = 128;

        public CityName(string? value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: DomainErrorsFactory.CityNameNullOrEmpty,
                trim: true,
                propertyName: nameof(Value));

            if (normalized.Length > MaxLength)
                throw DomainErrorsFactory.CityNameTooLong(
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
