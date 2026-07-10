namespace Matrix.Education.Contracts.Enrollments
{
    public sealed record EnrollStudentRequest(
        Guid ResidentId,
        Guid InstitutionId,
        string Stage,
        DateOnly EnrolledOn);

    public sealed record CompleteStudentStageRequest(
        Guid ResidentId,
        DateOnly CompletedOn);

    public sealed record WithdrawStudentRequest(
        Guid ResidentId,
        DateOnly WithdrawnOn);
}
