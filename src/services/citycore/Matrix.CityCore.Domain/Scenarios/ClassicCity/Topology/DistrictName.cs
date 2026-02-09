using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology
{
    /// <summary>
    ///     Human-readable district name within a city.
    /// </summary>
    public readonly record struct DistrictName
    {
        public const int MaxLength = 128;

        public DistrictName(string? value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: DomainErrorsFactory.DistrictNameNullOrEmpty,
                trim: true,
                propertyName: nameof(Value));

            if (normalized.Length > MaxLength)
                throw DomainErrorsFactory.DistrictNameTooLong(
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
