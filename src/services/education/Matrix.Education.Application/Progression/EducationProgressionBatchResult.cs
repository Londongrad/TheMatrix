using Matrix.Education.Application.Integration;

namespace Matrix.Education.Application.Progression
{
    public sealed record EducationProgressionBatchResult(
        int StudentProfilesEvaluated,
        int EnrollmentsStarted,
        int EnrollmentsCompleted,
        int EnrollmentsWithdrawn,
        int InstitutionsUpdated,
        IReadOnlyList<EducationStudentParticipationChange> ParticipationChanges)
    {
        public static EducationProgressionBatchResult Empty { get; } = new(
            StudentProfilesEvaluated: 0,
            EnrollmentsStarted: 0,
            EnrollmentsCompleted: 0,
            EnrollmentsWithdrawn: 0,
            InstitutionsUpdated: 0,
            ParticipationChanges: []);
    }
}
