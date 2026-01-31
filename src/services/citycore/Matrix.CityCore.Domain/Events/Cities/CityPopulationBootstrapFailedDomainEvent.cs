using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Domain.Events.Cities
{
    public sealed record CityPopulationBootstrapFailedDomainEvent(
        CityId CityId,
        Guid OperationId,
        string FailureCode,
        DateTimeOffset FailedAtUtc)
        : DomainEventBase;
}
