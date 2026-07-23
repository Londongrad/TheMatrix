namespace Matrix.Population.Application.Integration.Education;

public sealed record EducationAttendanceProjection(long SourceTickId, DateTimeOffset ObservedAtSimTimeUtc,
    decimal AttendanceIndex, decimal CommuteAccessibilityIndex);

public sealed record EducationAttendanceInput(Guid ResidentId, long ResidentLifecycleRevision,
    long ParticipationRevision, decimal AttendanceIndex, decimal CommuteAccessibilityIndex);
