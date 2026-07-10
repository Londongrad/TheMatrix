using MediatR;

namespace Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions
{
    public sealed record SynchronizeEducationInstitutionsCommand(
        Guid SimulationHostId,
        long SourceRevision,
        DateTimeOffset SynchronizedAtUtc,
        IReadOnlyCollection<SynchronizeEducationInstitutionItem> Institutions)
        : IRequest<SynchronizeEducationInstitutionsResult>;
}
