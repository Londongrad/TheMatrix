namespace Matrix.Education.Contracts.Events;

public sealed record EducationAttendanceEvaluatedV1(Guid ResidentId, long ResidentLifecycleRevision,
    long ParticipationRevision, decimal AttendanceIndex, decimal CommuteAccessibilityIndex);

public sealed record EducationAttendanceEvaluatedBatchV1(Guid SimulationHostId, long SourceTickId,
    DateTimeOffset ObservedAtSimTimeUtc, DateTimeOffset OccurredAtUtc,
    IReadOnlyList<EducationAttendanceEvaluatedV1> Residents);
