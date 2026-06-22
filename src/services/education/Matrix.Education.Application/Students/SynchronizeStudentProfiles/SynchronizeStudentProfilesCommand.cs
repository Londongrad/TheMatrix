using MediatR;

namespace Matrix.Education.Application.Students.SynchronizeStudentProfiles
{
    public sealed record SynchronizeStudentProfilesCommand(
        Guid SimulationHostId,
        DateTimeOffset SynchronizedAtUtc,
        IReadOnlyList<SynchronizeStudentProfileItem> Profiles)
        : IRequest<SynchronizeStudentProfilesResult>;
}
