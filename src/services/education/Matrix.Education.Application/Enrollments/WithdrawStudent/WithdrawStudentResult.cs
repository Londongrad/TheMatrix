namespace Matrix.Education.Application.Enrollments.WithdrawStudent
{
    public sealed record WithdrawStudentResult(
        WithdrawStudentStatus Status,
        Guid? EnrollmentId = null);
}
