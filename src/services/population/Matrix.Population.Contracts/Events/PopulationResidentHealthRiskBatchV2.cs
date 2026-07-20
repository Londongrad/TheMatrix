namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentHealthRiskBatchV2(
        Guid SimulationHostId,
        long SourceRevision,
        DateOnly PreviousDate,
        DateOnly CurrentDate,
        DateTimeOffset ObservedAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<PopulationResidentHealthRiskV2> Residents);
}
