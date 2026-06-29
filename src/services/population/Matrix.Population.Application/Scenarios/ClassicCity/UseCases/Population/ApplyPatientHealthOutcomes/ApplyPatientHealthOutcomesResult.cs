namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes
{
    public sealed record ApplyPatientHealthOutcomesResult(
        ApplyPatientHealthOutcomesStatus Status,
        int AppliedPatientCount,
        int IgnoredPatientCount,
        int StalePatientCount);
}
