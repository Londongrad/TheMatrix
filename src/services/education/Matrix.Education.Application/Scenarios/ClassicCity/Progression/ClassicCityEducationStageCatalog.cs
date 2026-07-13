using Matrix.Education.Domain.Programs;

namespace Matrix.Education.Application.Scenarios.ClassicCity.Progression
{
    public static class ClassicCityEducationStageCatalog
    {
        public static EducationStageKey Preschool { get; } = new("preschool");
        public static EducationStageKey Primary { get; } = new("primary");
        public static EducationStageKey LowerSecondary { get; } = new("lower-secondary");
        public static EducationStageKey UpperSecondary { get; } = new("upper-secondary");
        public static EducationStageKey Vocational { get; } = new("vocational");
        public static EducationStageKey Higher { get; } = new("higher");
        public static EducationStageKey Postgraduate { get; } = new("postgraduate");
    }
}
