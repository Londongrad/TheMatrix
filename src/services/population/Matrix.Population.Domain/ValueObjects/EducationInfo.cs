using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class EducationInfo
    {
        private EducationInfo() { }

        private EducationInfo(
            EducationLevel level,
            EducationInstitutionId? currentInstitutionId,
            CityAnchorId? currentInstitutionAnchorId)
        {
            Level = GuardHelper.AgainstInvalidEnum(
                value: level,
                propertyName: nameof(level));
            CurrentInstitutionId = currentInstitutionId;
            CurrentInstitutionAnchorId = currentInstitutionAnchorId;
        }

        public EducationLevel Level { get; }
        public EducationInstitutionId? CurrentInstitutionId { get; }
        public CityAnchorId? CurrentInstitutionAnchorId { get; }

        public static EducationInfo FromLevel(
            EducationLevel level,
            EducationInstitutionId? currentInstitutionId = null,
            CityAnchorId? currentInstitutionAnchorId = null)
        {
            return new EducationInfo(
                level: level,
                currentInstitutionId: currentInstitutionId,
                currentInstitutionAnchorId: currentInstitutionAnchorId);
        }

        public static EducationInfo None()
        {
            return new EducationInfo(
                level: EducationLevel.None,
                currentInstitutionId: null,
                currentInstitutionAnchorId: null);
        }

        public EducationInfo GraduateTo(
            EducationLevel newLevel,
            EducationInstitutionId? currentInstitutionId = null,
            CityAnchorId? currentInstitutionAnchorId = null)
        {
            EducationRules.ValidateTransition(
                from: Level,
                to: newLevel);
            return new EducationInfo(
                level: newLevel,
                currentInstitutionId: currentInstitutionId,
                currentInstitutionAnchorId: currentInstitutionAnchorId);
        }

        public EducationInfo AssignInstitution(
            EducationInstitutionId institutionId,
            CityAnchorId? currentInstitutionAnchorId = null)
        {
            return new EducationInfo(
                level: Level,
                currentInstitutionId: GuardHelper.AgainstNull(
                    value: institutionId,
                    propertyName: nameof(institutionId)),
                currentInstitutionAnchorId: currentInstitutionAnchorId);
        }

        public EducationInfo ClearInstitution()
        {
            return new EducationInfo(
                level: Level,
                currentInstitutionId: null,
                currentInstitutionAnchorId: null);
        }
    }
}
