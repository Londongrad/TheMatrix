namespace Matrix.Education.Contracts.Events
{
    public sealed record EducationStudentParticipationBatchV1(
        Guid SimulationHostId,
        DateOnly SnapshotDate,
        DateTimeOffset OccurredAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<EducationStudentParticipationV1> Students);
}
