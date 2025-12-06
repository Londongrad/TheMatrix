using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class EducationInfo
    {
        private EducationInfo()
        {
        }

        private EducationInfo(EducationLevel level)
        {
            Level = GuardHelper.AgainstInvalidEnum(value: level, propertyName: nameof(level));
        }

        public EducationLevel Level { get; }

        public static EducationInfo FromLevel(EducationLevel level) =>
            new(level);

        public static EducationInfo None() =>
            new(EducationLevel.None);

        public EducationInfo GraduateTo(EducationLevel newLevel)
        {
            EducationRules.ValidateTransition(from: Level, to: newLevel);
            return new EducationInfo(newLevel);
        }
    }
}
