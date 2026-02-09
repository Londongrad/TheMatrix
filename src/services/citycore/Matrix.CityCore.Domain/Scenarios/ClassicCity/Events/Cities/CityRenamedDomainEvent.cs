using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Events.Cities
{
    public sealed record CityRenamedDomainEvent(
        CityId CityId,
        CityName From,
        CityName To)
        : DomainEventBase;
}
