namespace Matrix.Healthcare.Contracts.Events
{
    public sealed record HealthcarePatientHealthOutcomeBatchV1(
        Guid SimulationHostId,
        long SourceRevision,
        DateTimeOffset OccurredAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<HealthcarePatientHealthOutcomeV1> Patients);
}
