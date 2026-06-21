using Matrix.Education.Domain.Common;

namespace Matrix.Education.Domain.Institutions
{
    public readonly record struct EducationInstitutionKindKey
    {
        public EducationInstitutionKindKey(string value)
        {
            Value = EducationKeyValidator.Validate(
                value: value,
                propertyName: nameof(Value));
        }

        public string Value { get; }

        public override string ToString() => Value ?? string.Empty;
    }
}
