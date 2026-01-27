using Matrix.CityCore.Domain.Cities;
using Matrix.BuildingBlocks.Domain.Events;

namespace Matrix.CityCore.Domain.Events.Cities
{
    public sealed record CityCreatedDomainEvent(
        CityId CityId,
        CityName Name,
        DateTimeOffset CreatedAtUtc)
        : DomainEventBase;
}
