namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes
{
    public sealed record PatientHealthOutcomeInput(
        Guid PatientId,
        int HealthScore,
        int HappinessDelta,
        int EnergyDelta,
        int StressDelta,
        long LifecycleRevision = 0,
        int FunctionalCapacityScore = 100);
}
