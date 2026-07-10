namespace Matrix.Education.Application.Enrollments.EnrollStudent
{
    public sealed record EnrollStudentResult(
        EnrollStudentStatus Status,
        Guid? EnrollmentId = null);
}
