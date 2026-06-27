namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentMedicalStateBatchV1(
        Guid SimulationHostId,
        long SourceRevision,
        DateTimeOffset ObservedAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<PopulationResidentMedicalStateV1> Residents);
}
