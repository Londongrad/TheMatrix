using MediatR;

namespace Matrix.Healthcare.Application.Facilities.SynchronizeCareFacilities
{
    public sealed record SynchronizeCareFacilitiesCommand(
        Guid SimulationHostId,
        long SourceRevision,
        DateTimeOffset SynchronizedAtUtc,
        IReadOnlyList<SynchronizeCareFacilityItem> Facilities)
        : IRequest<SynchronizeCareFacilitiesResult>;
}
