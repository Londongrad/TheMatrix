using Matrix.BuildingBlocks.Domain;
using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.ValueObjects
{
    public sealed record Username
    {
        public const int MinLength = 3;
        public const int MaxLength = 16;

        private Username(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static Username Create(string raw)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: raw,
                errorFactory: DomainErrorsFactory.EmptyUsername,
                trim: true,
                propertyName: nameof(Username));

            GuardHelper.AgainstOutOfRange(
                value: normalized.Length,
                min: MinLength,
                max: MaxLength,
                errorFactory: DomainErrorsFactory.InvalidUsernameLength,
                propertyName: nameof(Username));

            return new Username(normalized);
        }
    }
}
