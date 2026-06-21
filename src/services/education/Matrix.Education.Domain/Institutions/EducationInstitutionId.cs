using Matrix.BuildingBlocks.Domain;

namespace Matrix.Education.Domain.Institutions
{
    public readonly record struct EducationInstitutionId
    {
        public EducationInstitutionId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static EducationInstitutionId New() => new(Guid.NewGuid());

        public override string ToString() => Value.ToString();
    }
}
