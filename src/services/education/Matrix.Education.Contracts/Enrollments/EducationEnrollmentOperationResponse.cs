namespace Matrix.Education.Contracts.Enrollments
{
    public sealed record EducationEnrollmentOperationResponse(
        string Status,
        Guid? EnrollmentId = null,
        string? CompletedStage = null);
}
