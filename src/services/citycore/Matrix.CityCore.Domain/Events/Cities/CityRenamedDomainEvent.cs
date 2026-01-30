using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Domain.Events.Cities
{
    public sealed record CityRenamedDomainEvent(
        CityId CityId,
        CityName From,
        CityName To)
        : DomainEventBase;
}
