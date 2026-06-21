namespace Matrix.Education.Application.Progression
{
    public sealed record EducationProgressionBatchResult(
        int StudentProfilesEvaluated,
        int EnrollmentsStarted,
        int EnrollmentsCompleted,
        int EnrollmentsWithdrawn,
        int InstitutionsUpdated)
    {
        public static EducationProgressionBatchResult Empty { get; } = new(
            StudentProfilesEvaluated: 0,
            EnrollmentsStarted: 0,
            EnrollmentsCompleted: 0,
            EnrollmentsWithdrawn: 0,
            InstitutionsUpdated: 0);
    }
}
