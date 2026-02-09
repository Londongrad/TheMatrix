using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Events.Cities
{
    public sealed record CityEnvironmentChangedDomainEvent(
        CityId CityId,
        CityEnvironment From,
        CityEnvironment To)
        : DomainEventBase;
}
