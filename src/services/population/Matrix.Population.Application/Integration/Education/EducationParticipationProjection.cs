using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Integration.Education
{
    public sealed record EducationParticipationProjection(
        Guid SimulationHostId,
        Guid ResidentId,
        long ParticipationRevision,
        long ResidentLifecycleRevision,
        bool IsEnrolled,
        string? ActiveStage,
        Guid? InstitutionId,
        Guid? InstitutionAnchorId,
        DateOnly? EnrolledOn,
        string? CompletedStage,
        DateOnly? CompletedStageOn,
        DateOnly SnapshotDate,
        DateTimeOffset OccurredAtUtc,
        DateTimeOffset UpdatedAtUtc,
        ResidentExternalEconomicProfile? Economics = null,
        EducationAttendanceProjection? Attendance = null,
        PersonRoutineProfile? Routine = null);
}
