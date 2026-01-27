using Matrix.CityCore.Domain.Cities;
using Matrix.BuildingBlocks.Domain.Events;

namespace Matrix.CityCore.Domain.Events.Cities
{
    public sealed record CityRenamedDomainEvent(
        CityId CityId,
        CityName From,
        CityName To)
        : DomainEventBase;
}
