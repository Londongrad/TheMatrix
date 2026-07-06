namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentVitalStateBatchV1(
        Guid SimulationHostId,
        long SourceRevision,
        DateTimeOffset ObservedAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<PopulationResidentVitalStateV1> Residents);
}
