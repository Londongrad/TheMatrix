namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyResidentVitalStateOutcomes
{
    public sealed record ApplyResidentVitalStateOutcomesResult(
        ApplyResidentVitalStateOutcomesStatus Status,
        int AppliedResidentCount,
        int IgnoredResidentCount,
        int StaleResidentCount);
}
