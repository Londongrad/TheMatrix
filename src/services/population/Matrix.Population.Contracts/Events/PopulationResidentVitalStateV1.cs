namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentVitalStateV1(
        Guid ResidentId,
        int HealthScore,
        long LifecycleRevision = 0);
}
