using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class EducationInfo
    {
        private EducationInfo() { }

        private EducationInfo(
            EducationLevel level,
            EducationInstitutionId? currentInstitutionId)
        {
            Level = GuardHelper.AgainstInvalidEnum(
                value: level,
                propertyName: nameof(level));
            CurrentInstitutionId = currentInstitutionId;
        }

        public EducationLevel Level { get; }
        public EducationInstitutionId? CurrentInstitutionId { get; }

        public static EducationInfo FromLevel(
            EducationLevel level,
            EducationInstitutionId? currentInstitutionId = null)
        {
            return new EducationInfo(
                level: level,
                currentInstitutionId: currentInstitutionId);
        }

        public static EducationInfo None()
        {
            return new EducationInfo(
                level: EducationLevel.None,
                currentInstitutionId: null);
        }

        public EducationInfo GraduateTo(
            EducationLevel newLevel,
            EducationInstitutionId? currentInstitutionId = null)
        {
            EducationRules.ValidateTransition(
                from: Level,
                to: newLevel);
            return new EducationInfo(
                level: newLevel,
                currentInstitutionId: currentInstitutionId);
        }

        public EducationInfo AssignInstitution(EducationInstitutionId institutionId)
        {
            return new EducationInfo(
                level: Level,
                currentInstitutionId: GuardHelper.AgainstNull(
                    value: institutionId,
                    propertyName: nameof(institutionId)));
        }

        public EducationInfo ClearInstitution()
        {
            return new EducationInfo(
                level: Level,
                currentInstitutionId: null);
        }
    }
}
