using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology
{
    /// <summary>
    ///     Human-readable residential building name.
    /// </summary>
    public readonly record struct ResidentialBuildingName
    {
        public const int MaxLength = 160;

        public ResidentialBuildingName(string? value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.ResidentialBuildingNameNullOrEmpty,
                trim: true,
                propertyName: nameof(Value));

            if (normalized.Length > MaxLength)
                throw ClassicCityDomainErrorsFactory.ResidentialBuildingNameTooLong(
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
