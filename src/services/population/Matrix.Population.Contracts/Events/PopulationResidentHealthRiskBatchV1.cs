namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentHealthRiskBatchV1(
        Guid SimulationHostId,
        long SourceRevision,
        DateOnly PreviousDate,
        DateOnly CurrentDate,
        DateTimeOffset ObservedAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<PopulationResidentHealthRiskV1> Residents);
}
