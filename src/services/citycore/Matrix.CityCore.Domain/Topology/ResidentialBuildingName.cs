using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;

namespace Matrix.CityCore.Domain.Topology
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
                errorFactory: DomainErrorsFactory.ResidentialBuildingNameNullOrEmpty,
                trim: true,
                propertyName: nameof(Value));

            if (normalized.Length > MaxLength)
                throw DomainErrorsFactory.ResidentialBuildingNameTooLong(
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
