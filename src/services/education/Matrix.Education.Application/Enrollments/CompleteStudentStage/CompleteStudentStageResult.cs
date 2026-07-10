namespace Matrix.Education.Application.Enrollments.CompleteStudentStage
{
    public sealed record CompleteStudentStageResult(
        CompleteStudentStageStatus Status,
        Guid? EnrollmentId = null,
        string? CompletedStage = null);
}
