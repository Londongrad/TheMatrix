using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Domain.Events.Cities
{
    public sealed record CityDeletedDomainEvent(
        CityId CityId,
        DateTimeOffset DeletedAtUtc)
        : DomainEventBase;
}
