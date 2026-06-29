namespace Matrix.Healthcare.Application.Patients.AdvancePatientHealth
{
    public sealed record PatientHealthOutcomeBatch(
        Guid SimulationHostId,
        long SourceRevision,
        DateOnly CurrentDate,
        DateTimeOffset OccurredAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<PatientHealthProgressionResultItem> Patients);
}
