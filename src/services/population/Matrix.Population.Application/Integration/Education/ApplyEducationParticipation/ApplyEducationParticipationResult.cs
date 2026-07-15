namespace Matrix.Population.Application.Integration.Education.ApplyEducationParticipation
{
    public sealed record ApplyEducationParticipationResult(
        ApplyEducationParticipationStatus Status,
        int AppliedStudentCount = 0,
        int StaleStudentCount = 0,
        int MissingOrChangedResidentCount = 0);
}
