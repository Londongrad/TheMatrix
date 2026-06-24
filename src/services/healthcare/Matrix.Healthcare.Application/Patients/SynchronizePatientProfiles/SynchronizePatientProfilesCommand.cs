using MediatR;

namespace Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles
{
    public sealed record SynchronizePatientProfilesCommand(
        Guid SimulationHostId,
        DateTimeOffset SynchronizedAtUtc,
        IReadOnlyList<SynchronizePatientProfileItem> Profiles)
        : IRequest<SynchronizePatientProfilesResult>;
}
