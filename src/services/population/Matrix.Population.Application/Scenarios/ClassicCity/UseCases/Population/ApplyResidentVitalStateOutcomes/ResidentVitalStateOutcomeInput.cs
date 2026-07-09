namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyResidentVitalStateOutcomes
{
    public sealed record ResidentVitalStateOutcomeInput(
        Guid ResidentId,
        int HealthScore,
        int HappinessDelta,
        int EnergyDelta,
        int StressDelta,
        long LifecycleRevision = 0,
        int FunctionalCapacityScore = 100);
}
