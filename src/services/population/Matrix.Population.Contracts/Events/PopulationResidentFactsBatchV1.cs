namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentFactsBatchV1(
        Guid SimulationHostId,
        long SourceRevision,
        DateTimeOffset SynchronizedAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<PopulationResidentFactsV1> Residents);
}
