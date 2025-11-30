using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class EducationInfo
    {
        public EducationLevel Level { get; }

        private EducationInfo() { }

        private EducationInfo(EducationLevel level)
        {
            Level = GuardHelper.AgainstInvalidEnum(level, nameof(level));
        }

        public static EducationInfo FromLevel(EducationLevel level) =>
            new(level);

        public static EducationInfo None() =>
            new(EducationLevel.None);

        public EducationInfo GraduateTo(EducationLevel newLevel)
        {
            EducationRules.ValidateTransition(Level, newLevel);
            return new EducationInfo(newLevel);
        }
    }
}
