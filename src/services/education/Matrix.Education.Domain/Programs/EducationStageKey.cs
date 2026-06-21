using Matrix.Education.Domain.Common;

namespace Matrix.Education.Domain.Programs
{
    public readonly record struct EducationStageKey
    {
        public EducationStageKey(string value)
        {
            Value = EducationKeyValidator.Validate(
                value: value,
                propertyName: nameof(Value));
        }

        public string Value { get; }

        public override string ToString() => Value ?? string.Empty;
    }
}
