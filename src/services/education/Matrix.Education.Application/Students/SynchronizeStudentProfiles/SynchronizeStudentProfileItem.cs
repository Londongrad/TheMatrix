namespace Matrix.Education.Application.Students.SynchronizeStudentProfiles
{
    public sealed record SynchronizeStudentProfileItem(
        Guid ResidentId,
        DateOnly BirthDate,
        bool IsAlive,
        bool IsActive,
        long SourceRevision,
        long LifecycleRevision = 0);
}
