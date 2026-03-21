using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities
{
    public sealed record CityRenamedDomainEvent(
        CityId CityId,
        CityName From,
        CityName To)
        : DomainEventBase;
}
