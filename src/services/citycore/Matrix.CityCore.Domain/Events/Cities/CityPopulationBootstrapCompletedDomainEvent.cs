using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Domain.Events.Cities
{
    public sealed record CityPopulationBootstrapCompletedDomainEvent(
        CityId CityId,
        DateTimeOffset CompletedAtUtc)
        : DomainEventBase;
}
