using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed record EducationInstitutionId
    {
        public Guid Value { get; }

        private EducationInstitutionId(Guid value)
        {
            Value = value;
        }

        public static EducationInstitutionId New()
        {
            return new EducationInstitutionId(Guid.NewGuid());
        }

        public static EducationInstitutionId From(Guid value)
        {
            return new EducationInstitutionId(
                GuardHelper.AgainstEmptyGuid(
                    id: value,
                    propertyName: nameof(EducationInstitutionId)));
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
