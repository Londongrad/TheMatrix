using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed record EducationInstitutionId
    {
        private EducationInstitutionId(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }

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
